using System;
using System.Collections.Generic;
using System.Linq;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class UniversalReceivablesTests : TestBase
    {
        private readonly AuditService _auditService = new AuditService();
        private readonly SettingService _settingService = new SettingService();
        private readonly InventoryService _inventoryService;
        private readonly ProductService _productService;
        private readonly InstallmentSaleService _saleService;
        private readonly ReceivablesService _receivablesService = new ReceivablesService();

        public UniversalReceivablesTests()
        {
            _inventoryService = new InventoryService(_auditService);
            _productService = new ProductService(_auditService);
            _saleService = new InstallmentSaleService(_inventoryService, _auditService, _settingService);
        }

        [Fact]
        public void UnifiedLedger_SeamlesslyCombinesRentAndInstallmentTransactions()
        {
            // Create a party who is both a tenant and buys merchandise on installments
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(rent: 25000);

            // 1. Post monthly rent charge: 25,000
            BillingService.GenerateMonthlyRent(2026, 9);
            decimal bal1 = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(25000, bal1);

            // 2. Pay 15,000 against rent -> balance = 10,000
            PaymentService.RecordPayment(new PaymentDto
            {
                RentAgreementId = agreement.Id,
                TenantId = tenant.Id,
                PropertyUnitId = unit.Id,
                Amount = 15000,
                PaymentMethod = PaymentMethod.Cash,
                ReferenceNumber = "RCP-RENT",
                Remarks = "Rent payment"
            });
            decimal bal2 = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(10000, bal2);

            // 3. Sell refrigerator on installment: 80,000 with 20,000 down payment
            var items = new List<SaleItem>
            {
                new SaleItem { ItemDescription = "Refrigerator 18 cu ft", Quantity = 1, TotalPrice = 80000 }
            };
            var sale = new InstallmentSale
            {
                CustomerId = tenant.Id,
                SaleType = SaleType.InstallmentSale,
                NetSalePrice = 80000,
                DownPayment = 20000,
                NumberOfInstallments = 6,
                Frequency = InstallmentFrequency.Monthly,
                FirstDueDate = DateTime.Today.AddMonths(1)
            };
            var saleRes = _saleService.CreateInstallmentSale(sale, items, 1, "Admin");
            Assert.True(saleRes.Success);

            // Ledger balance should now be: 10,000 (pending rent) + 60,000 (net financed amount) = 70,000
            decimal bal3 = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(70000, bal3);

            // 4. Pay 10,000 installment
            var instPayRes = _saleService.CollectInstallmentPayment(sale.Id, 10000, PaymentMethod.Cash, "RCP-INST-1", "First installment", 1, "Admin");
            Assert.True(instPayRes.Success);

            // Ledger balance should now be: 70,000 - 10,000 = 60,000
            decimal bal4 = TenantService.GetTenantCurrentBalance(tenant.Id);
            Assert.Equal(60000, bal4);

            // Verify statement entries
            var statement = LedgerService.GetTenantLedger(tenant.Id);
            Assert.Equal(60000, statement.ClosingBalance);
            Assert.Equal(105000, statement.TotalDebit); // 25,000 rent + 80,000 sale invoice = 105,000
            Assert.Equal(45000, statement.TotalCredit); // 15,000 rent pay + 20,000 down pay + 10,000 inst pay = 45,000
        }

        [Fact]
        public void CustomerCreditProfile_CalculatesAccurateMetrics()
        {
            var customer = new Tenant
            {
                TenantCode = "CUST-PROFILE-01",
                FullName = "Kashif Mahmood",
                ContactNumber = "03451122334",
                CreditLimit = 150000,
                Rating = "Good",
                GuarantorName = "Tariq Mahmood",
                GuarantorPhone = "03004455667",
                GuarantorRelation = "Brother"
            };
            TenantService.SaveTenant(customer);

            var items = new List<SaleItem>
            {
                new SaleItem { ItemDescription = "Laptop Core i7", Quantity = 1, TotalPrice = 120000 }
            };
            var sale = new InstallmentSale
            {
                CustomerId = customer.Id,
                SaleType = SaleType.InstallmentSale,
                NetSalePrice = 120000,
                DownPayment = 30000,
                NumberOfInstallments = 9,
                Frequency = InstallmentFrequency.Monthly,
                FirstDueDate = DateTime.Today.AddMonths(1)
            };
            _saleService.CreateInstallmentSale(sale, items, 1, "Admin");

            var profile = _receivablesService.GetCustomerCreditProfile(customer.Id);
            Assert.NotNull(profile);
            Assert.Equal(120000, profile!.TotalPurchases);
            Assert.Equal(30000, profile.TotalPaid);
            Assert.Equal(90000, profile.CurrentOutstanding);
            Assert.Equal(150000, profile.CreditLimit);
            Assert.Equal(60000, profile.AvailableCredit); // 150,000 - 90,000 = 60,000
            Assert.Equal("Tariq Mahmood", profile.GuarantorName);
            Assert.Equal(1, profile.ActivePlansCount);
        }

        [Fact]
        public void TerminologyService_ReturnsAppropriateLabelsForModes()
        {
            var terminology = new TerminologyService(_settingService);

            SettingService.Set("Business.Type", BusinessType.PropertyRent.ToString());
            Assert.Equal("Tenant", terminology.CustomerSingular);
            Assert.Equal("Property / Unit", terminology.ItemSingular);
            Assert.Equal("Rent Agreement", terminology.AgreementOrSale);

            SettingService.Set("Business.Type", BusinessType.InstallmentRetail.ToString());
            Assert.Equal("Customer", terminology.CustomerSingular);
            Assert.Equal("Product", terminology.ItemSingular);
            Assert.Equal("Installment Sale", terminology.AgreementOrSale);
        }
    }
}
