using System;
using System.Linq;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class FinancialCalculationTests : TestBase
    {
        [Fact]
        public void Scenario1_ExactRentPayment_ResultsInZeroBalance()
        {
            // Rent = 30,000, Payment = 30,000 => Balance = 0
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);

            // Generate Rent for September 2026
            var genRes = BillingService.GenerateMonthlyRent(2026, 9);
            Assert.Equal(1, genRes.GeneratedCount);
            Assert.Equal(30000, genRes.TotalDemanded);

            // Check balance before payment
            decimal balBefore = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(30000, balBefore);

            // Record Exact Payment of 30,000
            var payDto = new PaymentDto
            {
                TenantId = tenant.Id,
                RentAgreementId = agreement.Id,
                PropertyUnitId = unit.Id,
                Amount = 30000,
                PaymentDate = new DateTime(2026, 9, 5),
                PaymentMethod = PaymentMethod.Cash,
                RentalPeriod = "September 2026"
            };

            var payRes = PaymentService.RecordPayment(payDto);
            Assert.True(payRes.Success);
            Assert.NotNull(payRes.Receipt);
            Assert.Equal(30000, payRes.Receipt.PreviousBalance);
            Assert.Equal(30000, payRes.Receipt.CurrentPayment);
            Assert.Equal(0, payRes.Receipt.RemainingBalance);

            // Verify Ledger Balance
            decimal balAfter = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(0, balAfter);

            // Verify Rent Schedule Status
            using var db = new AppDbContext();
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            Assert.Equal(30000, schedule.AmountPaid);
            Assert.Equal(0, schedule.Balance);
            Assert.Equal(RentScheduleStatus.Paid, schedule.Status);
        }

        [Fact]
        public void Scenario2_PartialPayment_CalculatesCorrectRemainingBalance()
        {
            // Rent = 30,000, Payment = 20,000 => Balance = 10,000
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);

            BillingService.GenerateMonthlyRent(2026, 9);

            var payDto = new PaymentDto
            {
                TenantId = tenant.Id,
                RentAgreementId = agreement.Id,
                Amount = 20000,
                PaymentDate = new DateTime(2026, 9, 5),
                PaymentMethod = PaymentMethod.Cash
            };

            var payRes = PaymentService.RecordPayment(payDto);
            Assert.True(payRes.Success);
            Assert.Equal(30000, payRes.Receipt!.PreviousBalance);
            Assert.Equal(20000, payRes.Receipt.CurrentPayment);
            Assert.Equal(10000, payRes.Receipt.RemainingBalance);

            decimal currentBal = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(10000, currentBal);

            using var db = new AppDbContext();
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            Assert.Equal(20000, schedule.AmountPaid);
            Assert.Equal(10000, schedule.Balance);
            Assert.Equal(RentScheduleStatus.Partial, schedule.Status);
        }

        [Fact]
        public void Scenario3_MultipleInstallments_SequentiallyReducesBalanceToZero()
        {
            // Rent = 30,000, Pay 1 = 10,000, Pay 2 = 10,000, Pay 3 = 10,000 => Balance = 0
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);
            BillingService.GenerateMonthlyRent(2026, 9);

            // Payment 1: 10,000
            var pay1 = PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 10000, PaymentDate = new DateTime(2026, 9, 2) });
            Assert.True(pay1.Success);
            Assert.Equal(20000, pay1.Receipt!.RemainingBalance);
            Assert.Equal(20000, TenantService.GetTenantCurrentBalance(tenant.Id));

            // Payment 2: 10,000
            var pay2 = PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 10000, PaymentDate = new DateTime(2026, 9, 10) });
            Assert.True(pay2.Success);
            Assert.Equal(10000, pay2.Receipt!.RemainingBalance);
            Assert.Equal(10000, TenantService.GetTenantCurrentBalance(tenant.Id));

            // Payment 3: 10,000
            var pay3 = PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 10000, PaymentDate = new DateTime(2026, 9, 20) });
            Assert.True(pay3.Success);
            Assert.Equal(0, pay3.Receipt!.RemainingBalance);
            Assert.Equal(0, TenantService.GetTenantCurrentBalance(tenant.Id));

            // Verify Schedule
            using var db = new AppDbContext();
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            Assert.Equal(30000, schedule.AmountPaid);
            Assert.Equal(0, schedule.Balance);
            Assert.Equal(RentScheduleStatus.Paid, schedule.Status);
        }

        [Fact]
        public void Scenario4_Overpayment_CreatesAdvanceCreditBalance()
        {
            // Rent = 30,000, Payment = 40,000 => Net Balance = -10,000 (Advance credit)
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);
            BillingService.GenerateMonthlyRent(2026, 9);

            var pay = PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 40000, PaymentDate = new DateTime(2026, 9, 5) });
            Assert.True(pay.Success);
            Assert.Equal(-10000, pay.Receipt!.RemainingBalance);

            decimal netBal = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(-10000, netBal);

            using var db = new AppDbContext();
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            Assert.Equal(30000, schedule.AmountPaid);
            Assert.Equal(0, schedule.Balance);
            Assert.Equal(RentScheduleStatus.Paid, schedule.Status);
        }

        [Fact]
        public void Scenario5_VoidTransaction_PreservesAuditTrailAndRestoresBalance()
        {
            // Rent = 30,000, Pay = 20,000 (Bal = 10,000) -> Void Pay -> Balance restored to 30,000
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);
            BillingService.GenerateMonthlyRent(2026, 9);

            var pay = PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 20000, PaymentDate = new DateTime(2026, 9, 5) });
            Assert.True(pay.Success);
            Assert.Equal(10000, TenantService.GetTenantCurrentBalance(tenant.Id));

            // Void the payment transaction
            int txId = pay.Receipt!.TransactionId;
            var voidRes = PaymentService.VoidTransaction(txId, "Payment entered for wrong tenant by mistake");
            Assert.True(voidRes.Success);

            // Balance must be restored to 30,000
            decimal restoredBal = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(30000, restoredBal);

            // Check that transaction was NOT physically deleted
            using var db = new AppDbContext();
            var originalTx = db.Transactions.Find(txId);
            Assert.NotNull(originalTx);
            Assert.True(originalTx.IsVoided);
            Assert.Equal("Payment entered for wrong tenant by mistake", originalTx.VoidReason);

            // Check that an Adjustment reversal transaction was created
            var reversalTx = db.Transactions.FirstOrDefault(t => t.TransactionType == TransactionType.Adjustment && t.TenantId == tenant.Id);
            Assert.NotNull(reversalTx);
            Assert.Contains("Reversal", reversalTx.Description);

            // Check that rent schedule was reverted to Pending / Overdue
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            Assert.Equal(0, schedule.AmountPaid);
            Assert.Equal(30000, schedule.Balance);
        }

        [Fact]
        public void Scenario6_RentRateIncrease_PreservesHistoryAndAppliesToNewMonths()
        {
            // Rent starts at 30,000 in Sept -> increases to 35,000 in Oct
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);

            // Generate Sept rent
            BillingService.GenerateMonthlyRent(2026, 9);

            // Update rent rate to 35,000
            var updateRes = LeaseService.UpdateRentRate(agreement.Id, 35000, new DateTime(2026, 10, 1), "Annual 10% rent increment");
            Assert.True(updateRes.Success);

            // Generate Oct rent
            BillingService.GenerateMonthlyRent(2026, 10);

            using var db = new AppDbContext();
            var septSchedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");
            var octSchedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-10");

            Assert.Equal(30000, septSchedule.TotalDue);
            Assert.Equal(35000, octSchedule.TotalDue);

            // Check historical log
            var rateHistories = db.RentRateHistories.Where(r => r.RentAgreementId == agreement.Id).ToList();
            Assert.Equal(2, rateHistories.Count); // Initial 30k + Update 35k
            Assert.Equal(35000, rateHistories.Last().NewRent);
        }

        [Fact]
        public void Scenario7_ExtraCharges_UpdatesScheduleAndPostsLedgerDebit()
        {
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);
            BillingService.GenerateMonthlyRent(2026, 9);

            using var db = new AppDbContext();
            var schedule = db.RentSchedules.First(s => s.RentAgreementId == agreement.Id && s.MonthYear == "2026-09");

            // Add Utility 5,000 + Maintenance 2,000
            var chargeRes = BillingService.UpdateScheduleCharges(schedule.Id, 5000, 2000, 0);
            Assert.True(chargeRes.Success);

            using (var verifyDb = new AppDbContext())
            {
                var updatedSchedule = verifyDb.RentSchedules.Find(schedule.Id);
                Assert.Equal(37000, updatedSchedule!.TotalDue);
                Assert.Equal(37000, updatedSchedule.Balance);
            }

            decimal currentBal = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(37000, currentBal);
        }
    }
}
