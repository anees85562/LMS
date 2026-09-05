using System;
using System.IO;
using System.Linq;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class RegisterAndReportingTests : TestBase
    {
        [Fact]
        public void TraditionalRegisterMatrix_CorrectlyCalculatesArrearsAndCurrentDemands()
        {
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);

            // Month 1 (August): Generate rent 30,000, Pay 10,000 -> Arrears = 20,000
            BillingService.GenerateMonthlyRent(2026, 8);
            PaymentService.RecordPayment(new PaymentDto
            {
                TenantId = tenant.Id,
                Amount = 10000,
                PaymentDate = new DateTime(2026, 8, 5),
                RentalPeriod = "August 2026"
            });

            // Month 2 (September): Generate rent 30,000
            BillingService.GenerateMonthlyRent(2026, 9);

            // Fetch September Register Matrix
            var matrix = LedgerService.GetTraditionalRegisterMatrix(2026, 9, prop.Id);
            Assert.Single(matrix);

            var row = matrix.First();
            Assert.Equal(30000, row.CurrentRentDemanded);
            Assert.Equal(20000, row.PreviousArrears);
            Assert.Equal(50000, row.TotalDemanded); // 30k current + 20k arrears
            Assert.Equal(0, row.PaidThisMonth);
            Assert.Equal(50000, row.NetBalance);

            // Now make a payment of 25,000 in September
            PaymentService.RecordPayment(new PaymentDto
            {
                TenantId = tenant.Id,
                Amount = 25000,
                PaymentDate = new DateTime(2026, 9, 10),
                RentalPeriod = "September 2026"
            });

            // Re-fetch September Register Matrix
            var updatedMatrix = LedgerService.GetTraditionalRegisterMatrix(2026, 9, prop.Id);
            var updatedRow = updatedMatrix.First();
            Assert.Equal(25000, updatedRow.PaidThisMonth);
            Assert.Equal(25000, updatedRow.NetBalance); // 50k demanded - 25k paid = 25k balance
            Assert.Equal("Partial", updatedRow.Status);
        }

        [Fact]
        public void Reports_GenerateAccurateDatasetsForReportsAndExports()
        {
            var (landlord, prop, unit, tenant, agreement) = SeedBasicTenancy(30000);
            BillingService.GenerateMonthlyRent(2026, 9);
            PaymentService.RecordPayment(new PaymentDto { TenantId = tenant.Id, Amount = 15000, PaymentDate = new DateTime(2026, 9, 5) });

            // 1. Monthly Rent Report
            var rentReport = ReportService.GetMonthlyRentReport(2026, 9);
            Assert.Equal(1, rentReport.Data.Rows.Count);
            Assert.Equal(30000m, rentReport.Data.Rows[0]["Total Demanded"]);
            Assert.Equal(15000m, rentReport.Data.Rows[0]["Amount Paid"]);

            // 2. Tenant Statement Report
            var stmtReport = ReportService.GetTenantStatementReport(tenant.Id);
            Assert.Equal(2, stmtReport.Data.Rows.Count); // 1 Debit (Rent) + 1 Credit (Payment)

            // 3. Vacancy Report
            var vacancyReport = ReportService.GetVacancyReport();
            Assert.Equal(1, vacancyReport.Data.Rows.Count);
            Assert.Equal("Occupied", vacancyReport.Data.Rows[0]["Status"]);

            // 4. Test Export to CSV & HTML
            string csvPath = Path.Combine(TempBackupDir, "test_export.csv");
            string htmlPath = Path.Combine(TempBackupDir, "test_export.html");

            ImportExportService.ExportToCsv(rentReport.Data, csvPath);
            Assert.True(File.Exists(csvPath));
            Assert.NotEmpty(File.ReadAllText(csvPath));

            ImportExportService.ExportToHtmlExcel(rentReport.Data, rentReport.Title, htmlPath);
            Assert.True(File.Exists(htmlPath));
            Assert.Contains("Monthly Rent Collection Sheet", File.ReadAllText(htmlPath));
        }

        [Fact]
        public void BulkDataImport_ImportsTenantsAndPropertiesCorrectly()
        {
            string tenantCsv = Path.Combine(TempBackupDir, "tenants.csv");
            File.WriteAllText(tenantCsv, ImportExportService.GenerateTenantCsvTemplate());

            var tenantRes = ImportExportService.ImportTenantsFromCsv(tenantCsv);
            Assert.True(tenantRes.Success);
            Assert.Equal(2, tenantRes.ImportedCount);

            using (var db = new AppDbContext())
            {
                Assert.Equal(2, db.Tenants.Count());
            }

            string propCsv = Path.Combine(TempBackupDir, "properties.csv");
            File.WriteAllText(propCsv, ImportExportService.GeneratePropertyCsvTemplate());

            var propRes = ImportExportService.ImportPropertiesFromCsv(propCsv);
            Assert.True(propRes.Success);
            Assert.Equal(2, propRes.ImportedProps);
            Assert.Equal(4, propRes.ImportedUnits);

            using (var db = new AppDbContext())
            {
                Assert.Equal(2, db.Properties.Count());
                Assert.Equal(4, db.PropertyUnits.Count());
            }
        }
    }
}
