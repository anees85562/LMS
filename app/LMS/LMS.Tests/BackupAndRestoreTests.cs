using System;
using System.IO;
using System.Linq;
using LMS.Data;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class BackupAndRestoreTests : TestBase
    {
        [Fact]
        public void BackupCreation_CreatesValidIntegrityCheckedFile()
        {
            SeedBasicTenancy();

            var backupRes = BackupService.CreateBackup(TempBackupDir, BackupType.Manual, "Test Backup");
            Assert.True(backupRes.Success);
            Assert.NotNull(backupRes.BackupFilePath);
            Assert.True(File.Exists(backupRes.BackupFilePath));

            // Verify integrity
            bool isValid = BackupService.VerifyDatabaseIntegrity(backupRes.BackupFilePath);
            Assert.True(isValid);

            // Inspect backup contents
            var inspection = BackupService.InspectBackupFile(backupRes.BackupFilePath);
            Assert.True(inspection.IsValid);
            Assert.Equal(1, inspection.PropertyCount);
            Assert.Equal(1, inspection.TenantCount);
        }

        [Fact]
        public void SafeRestore_RestoresDatabaseToBackupStateAndCreatesPreRestoreSnapshot()
        {
            // Authenticate as Admin so permission checks pass
            AuthService.CreateUser("admin", "admin123", "Admin User", UserRole.Administrator);
            AuthService.Authenticate("admin", "admin123");

            // Step 1: Seed initial data
            SeedBasicTenancy();

            // Step 2: Take backup
            var backupRes = BackupService.CreateBackup(TempBackupDir, BackupType.Manual, "Initial State Backup");
            Assert.True(backupRes.Success);
            string backupFile = backupRes.BackupFilePath!;

            // Step 3: Add extra records that should NOT be in the restored database
            var extraTenant = new Tenant
            {
                TenantCode = "TEN-EXTRA-99",
                FullName = "Temporary Extra Tenant",
                ContactNumber = "03330000000",
                Status = TenantStatus.Active
            };
            TenantService.SaveTenant(extraTenant);

            using (var db = new AppDbContext())
            {
                Assert.Equal(2, db.Tenants.Count());
            }

            // Step 4: Perform Restore
            var restoreRes = BackupService.RestoreBackup(backupFile);
            Assert.True(restoreRes.Success);

            // Step 5: Verify restored database matches original backup state (1 tenant only)
            using (var db = new AppDbContext())
            {
                Assert.Equal(1, db.Tenants.Count());
                Assert.Equal("TEN-TEST-01", db.Tenants.First().TenantCode);
            }

            // Step 6: Verify pre-restore safety snapshot directory exists and has snapshot
            string snapshotDir = Path.Combine(TempBackupDir, "PreRestoreSnapshots");
            Assert.True(Directory.Exists(snapshotDir));
            Assert.NotEmpty(Directory.GetFiles(snapshotDir, "PreRestore_SafetySnapshot_*.db"));
        }

        [Fact]
        public void CorruptFileRestore_IsRejectedSafely()
        {
            AuthService.CreateUser("admin", "admin123", "Admin User", UserRole.Administrator);
            AuthService.Authenticate("admin", "admin123");

            SeedBasicTenancy();

            // Create a fake corrupt file
            string corruptFile = Path.Combine(TempBackupDir, "corrupted_fake.db");
            File.WriteAllText(corruptFile, "THIS IS NOT A VALID SQLITE DATABASE FILE HEADER");

            var restoreRes = BackupService.RestoreBackup(corruptFile);
            Assert.False(restoreRes.Success);
            Assert.Contains("not a valid", restoreRes.Message, StringComparison.OrdinalIgnoreCase);

            // Verify active database is intact
            using (var db = new AppDbContext())
            {
                Assert.Equal(1, db.Tenants.Count());
            }
        }

        [Fact]
        public void BackupRetention_NeverDeletesTheOnlyBackup()
        {
            SeedBasicTenancy();

            var backup = BackupService.CreateBackup(TempBackupDir, BackupType.Daily);
            Assert.True(backup.Success);

            // Set retention days to 0 and attempt cleanup
            SettingService.Set("Backup.RetentionDays", "0");
            BackupService.CleanupOldBackups(TempBackupDir);

            // The single backup must NOT be deleted
            var files = Directory.GetFiles(TempBackupDir, "Backup_*.db");
            Assert.Single(files);
        }
    }
}
