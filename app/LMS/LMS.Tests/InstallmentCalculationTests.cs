using System;
using System.Collections.Generic;
using System.Linq;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class InstallmentCalculationTests : TestBase
    {
        private readonly AuditService _auditService = new AuditService();
        private readonly SettingService _settingService = new SettingService();
        private readonly InventoryService _inventoryService;
        private readonly ProductService _productService;
        private readonly InstallmentSaleService _saleService;

        public InstallmentCalculationTests()
        {
            _inventoryService = new InventoryService(_auditService);
            _productService = new ProductService(_auditService);
            _saleService = new InstallmentSaleService(_inventoryService, _auditService, _settingService);
        }

        [Fact]
        public void PromptSpecification_InstallmentCalculationFlow_MatchesExactRequirements()
        {
            // Scenario from prompt:
            // Installment Sale Price = 100,000
            // Down Payment = 20,000
            // Remaining = 80,000
            // 8 Installments = 10,000 each

            var customer = new Tenant
            {
                TenantCode = "CUST-CRITIC-01",
                FullName = "Ahmed Khan",
                ContactNumber = "03009998877",
                CustomerType = CustomerType.InstallmentCustomer,
                Status = TenantStatus.Active
            };
            TenantService.SaveTenant(customer);

            var items = new List<SaleItem>
            {
                new SaleItem
                {
                    ItemDescription = "Samsung Galaxy S24 Ultra",
                    Quantity = 1,
                    UnitPrice = 100000,
                    InstallmentPrice = 100000,
                    TotalPrice = 100000
                }
            };

            var sale = new InstallmentSale
            {
                CustomerId = customer.Id,
                SaleType = SaleType.InstallmentSale,
                SaleDate = new DateTime(2026, 10, 1),
                NetSalePrice = 100000,
                DownPayment = 20000,
                NumberOfInstallments = 8,
                Frequency = InstallmentFrequency.Monthly,
                FirstDueDate = new DateTime(2026, 11, 5)
            };

            var createRes = _saleService.CreateInstallmentSale(sale, items, 1, "Admin");
            Assert.True(createRes.Success);
            Assert.Equal(80000, sale.FinancedAmount);
            Assert.Equal(20000, sale.TotalPaid);
            Assert.Equal(80000, sale.RemainingBalance);
            Assert.Equal(8, sale.Schedules.Count);

            // Verify schedules: 8 installments of 10,000 each
            foreach (var sch in sale.Schedules)
            {
                Assert.Equal(10000, sch.DueAmount);
                Assert.Equal(0, sch.PaidAmount);
                Assert.Equal(10000, sch.RemainingAmount);
                Assert.Equal(InstallmentItemStatus.Pending, sch.Status);
            }

            // Pay 3 complete installments: 3 x 10,000 = 30,000
            for (int i = 0; i < 3; i++)
            {
                var payRes = _saleService.CollectInstallmentPayment(sale.Id, 10000, PaymentMethod.Cash, $"CHK-{i + 1}", $"Installment {i + 1}", 1, "Admin");
                Assert.True(payRes.Success);
            }

            var refreshedSale = _saleService.GetInstallmentSaleById(sale.Id);
            Assert.NotNull(refreshedSale);
            // After 3 installments: Paid = 20,000 Down + 30,000 Installments = 50,000; Outstanding = 50,000
            Assert.Equal(50000, refreshedSale!.TotalPaid);
            Assert.Equal(50000, refreshedSale.RemainingBalance);

            // Now make a partial fourth payment: Fourth due = 10,000, Paid = 6,000
            var fourthPayRes = _saleService.CollectInstallmentPayment(sale.Id, 6000, PaymentMethod.Cash, "PART-4", "Partial payment", 1, "Admin");
            Assert.True(fourthPayRes.Success);

            refreshedSale = _saleService.GetInstallmentSaleById(sale.Id);
            Assert.NotNull(refreshedSale);

            // Prompt requirement: Current installment remaining = 4,000, Total outstanding = 44,000
            var fourthSchedule = refreshedSale!.Schedules.OrderBy(s => s.InstallmentNumber).Skip(3).First();
            Assert.Equal(4, fourthSchedule.InstallmentNumber);
            Assert.Equal(6000, fourthSchedule.PaidAmount);
            Assert.Equal(4000, fourthSchedule.RemainingAmount);
            Assert.Equal(InstallmentItemStatus.Partial, fourthSchedule.Status);

            Assert.Equal(56000, refreshedSale.TotalPaid); // 20,000 down + 30,000 (3 inst) + 6,000 (partial) = 56,000
            Assert.Equal(44000, refreshedSale.RemainingBalance); // 100,000 - 56,000 = 44,000

            // Customer ledger balance should also be exactly 44,000
            decimal customerBal = TenantService.GetTenantCurrentBalance(customer.Id);
            Assert.Equal(44000, customerBal);
        }

        [Fact]
        public void MultipleInstallments_PaidTogether_AllocatesAcrossSchedulesAccurately()
        {
            var customer = new Tenant
            {
                TenantCode = "CUST-MULTI-01",
                FullName = "Bilal Tariq",
                ContactNumber = "03211112233",
                CustomerType = CustomerType.InstallmentCustomer
            };
            TenantService.SaveTenant(customer);

            var items = new List<SaleItem>
            {
                new SaleItem { ItemDescription = "LED TV 55 Inch", Quantity = 1, TotalPrice = 60000 }
            };

            var sale = new InstallmentSale
            {
                CustomerId = customer.Id,
                SaleType = SaleType.InstallmentSale,
                NetSalePrice = 60000,
                DownPayment = 0,
                NumberOfInstallments = 6,
                Frequency = InstallmentFrequency.Monthly,
                FirstDueDate = DateTime.Today.AddDays(15)
            };

            var res = _saleService.CreateInstallmentSale(sale, items, 1, "Admin");
            Assert.True(res.Success);

            // Customer pays 25,000 (Installments 1 and 2 of 10,000 each + 5,000 of Installment 3)
            var payRes = _saleService.CollectInstallmentPayment(sale.Id, 25000, PaymentMethod.BankTransfer, "TX-25K", "Lump sum payment", 1, "Admin");
            Assert.True(payRes.Success);

            var refreshed = _saleService.GetInstallmentSaleById(sale.Id);
            Assert.NotNull(refreshed);

            var schedules = refreshed!.Schedules.OrderBy(s => s.InstallmentNumber).ToList();
            Assert.Equal(InstallmentItemStatus.Paid, schedules[0].Status);
            Assert.Equal(10000, schedules[0].PaidAmount);
            Assert.Equal(0, schedules[0].RemainingAmount);

            Assert.Equal(InstallmentItemStatus.Paid, schedules[1].Status);
            Assert.Equal(10000, schedules[1].PaidAmount);
            Assert.Equal(0, schedules[1].RemainingAmount);

            Assert.Equal(InstallmentItemStatus.Partial, schedules[2].Status);
            Assert.Equal(5000, schedules[2].PaidAmount);
            Assert.Equal(5000, schedules[2].RemainingAmount);

            Assert.Equal(InstallmentItemStatus.Pending, schedules[3].Status);
            Assert.Equal(35000, refreshed.RemainingBalance);
            Assert.Equal(25000, refreshed.TotalPaid);
        }

        [Fact]
        public void EarlySettlement_WithDiscount_CompletesPlanAndClearsBalance()
        {
            var customer = new Tenant
            {
                TenantCode = "CUST-EARLY-01",
                FullName = "Usman Ali",
                ContactNumber = "03335556677"
            };
            TenantService.SaveTenant(customer);

            var items = new List<SaleItem>
            {
                new SaleItem { ItemDescription = "Motorcycle 70cc", Quantity = 1, TotalPrice = 120000 }
            };

            var sale = new InstallmentSale
            {
                CustomerId = customer.Id,
                SaleType = SaleType.InstallmentSale,
                NetSalePrice = 120000,
                DownPayment = 20000,
                NumberOfInstallments = 10,
                Frequency = InstallmentFrequency.Monthly,
                FirstDueDate = DateTime.Today.AddMonths(1)
            };

            _saleService.CreateInstallmentSale(sale, items, 1, "Admin");

            // Remaining is 100,000. Customer wants early settlement: pays 95,000 with 5,000 discount.
            var settleRes = _saleService.EarlySettleSale(sale.Id, 95000, 5000, PaymentMethod.Cash, "Full early payment", 1, "Admin");
            Assert.True(settleRes.Success);

            var refreshed = _saleService.GetInstallmentSaleById(sale.Id);
            Assert.NotNull(refreshed);
            Assert.Equal(0, refreshed!.RemainingBalance);
            Assert.Equal(InstallmentPlanStatus.Settled, refreshed.Status);
            Assert.All(refreshed.Schedules, s => Assert.Equal(InstallmentItemStatus.Paid, s.Status));

            // Customer ledger should balance out to 0
            decimal bal = TenantService.GetTenantCurrentBalance(customer.Id);
            Assert.Equal(0, bal);
        }
    }
}
