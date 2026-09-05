using System;
using System.IO;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LMS.Tests
{
    public class TestBase : IDisposable
    {
        private static readonly object _dbLock = new();
        protected string TempDbPath { get; }
        protected string TempBackupDir { get; }

        public TestBase()
        {
            lock (_dbLock)
            {
                string testDir = Path.Combine(Path.GetTempPath(), "LMS_Tests_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(testDir);

                TempDbPath = Path.Combine(testDir, "test_landlord.db");
                TempBackupDir = Path.Combine(testDir, "backups");
                Directory.CreateDirectory(TempBackupDir);

                AppDbContext.DatabaseFilePath = TempDbPath;
                AppDbContext.InitializeDatabase();
                SettingService.ReloadCache();

                SettingService.Set("Backup.DefaultDirectory", TempBackupDir);
            }
        }

        protected (Landlord Landlord, Property Property, PropertyUnit Unit, Tenant Tenant, RentAgreement Agreement) SeedBasicTenancy(decimal rent = 30000)
        {
            lock (_dbLock)
            {
                AppDbContext.DatabaseFilePath = TempDbPath;
                var landlord = PropertyService.GetOrCreateDefaultLandlord();
                landlord.Name = "Test Owner";
                PropertyService.SaveLandlord(landlord);

                var prop = new Property
                {
                    LandlordId = landlord.Id,
                    PropertyCode = "PROP-TEST-01",
                    Name = "Grand Heights Plaza",
                    PropertyType = "Commercial",
                    City = "Lahore",
                    Status = PropertyStatus.Active
                };
                PropertyService.SaveProperty(prop);

                var unit = new PropertyUnit
                {
                    PropertyId = prop.Id,
                    UnitNumber = "Shop 101",
                    UnitType = "Shop",
                    Floor = "Ground Floor",
                    BaseRent = rent,
                    Status = UnitStatus.Vacant
                };
                PropertyService.SaveUnit(unit);

                var tenant = new Tenant
                {
                    TenantCode = "TEN-TEST-01",
                    FullName = "Ahmad Hassan",
                    ContactNumber = "03001234567",
                    CnicOrId = "35201-1234567-1",
                    Status = TenantStatus.Active
                };
                TenantService.SaveTenant(tenant);

                var agreement = new RentAgreement
                {
                    TenantId = tenant.Id,
                    PropertyUnitId = unit.Id,
                    AgreementCode = "AGR-TEST-01",
                    StartDate = new DateTime(2026, 1, 1),
                    EndDate = new DateTime(2026, 12, 31),
                    MonthlyRent = rent,
                    SecurityDeposit = rent * 2,
                    AdvanceAmount = 0,
                    DueDayOfMonth = 5,
                    RentIncrementRatePercent = 10,
                    Status = AgreementStatus.Active
                };
                LeaseService.CreateAgreement(agreement, postInitialDepositTransactions: false);

                return (landlord, prop, unit, tenant, agreement);
            }
        }

        public void Dispose()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                string? dir = Path.GetDirectoryName(TempDbPath);
                if (dir != null && Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { }
        }
    }
}
