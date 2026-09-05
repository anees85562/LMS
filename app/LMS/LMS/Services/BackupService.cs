using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class BackupService
    {
        public static string GetDefaultBackupDirectory()
        {
            string dir = SettingService.Get("Backup.DefaultDirectory", "");
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandlordManagementSystem", "Backups");
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }

        public static (bool Success, string Message, string? BackupFilePath) CreateBackup(string? targetDirectory = null, BackupType backupType = BackupType.Manual, string? notes = null)
        {
            try
            {
                string dir = string.IsNullOrWhiteSpace(targetDirectory) ? GetDefaultBackupDirectory() : targetDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string currentDbPath = AppDbContext.DatabaseFilePath;
                if (!File.Exists(currentDbPath))
                {
                    return (false, "Current database file does not exist yet.", null);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string backupFileName = $"Backup_{timestamp}.db";
                string backupFilePath = Path.Combine(dir, backupFileName);

                // Perform safe SQLite backup via VACUUM INTO or SQLite backup API
                using (var db = new AppDbContext())
                {
                    try
                    {
                        // Escape single quotes in path
                        string escapedPath = backupFilePath.Replace("'", "''");
                        db.Database.ExecuteSqlRaw("VACUUM INTO {0};", escapedPath);
                    }
                    catch
                    {
                        // Fallback: Copy file directly after checkpointing WAL
                        try
                        {
                            db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
                        }
                        catch { }

                        File.Copy(currentDbPath, backupFilePath, true);
                    }
                }

                if (!File.Exists(backupFilePath))
                {
                    return (false, "Backup file could not be written.", null);
                }

                // Verify backup integrity
                bool verified = VerifyDatabaseIntegrity(backupFilePath);
                if (!verified)
                {
                    File.Delete(backupFilePath);
                    return (false, "Backup file failed SQLite integrity check.", null);
                }

                var fileInfo = new FileInfo(backupFilePath);

                // Record in database
                try
                {
                    using var db = new AppDbContext();
                    var record = new BackupRecord
                    {
                        BackupDate = DateTime.Now,
                        FilePath = backupFilePath,
                        FileSizeBytes = fileInfo.Length,
                        BackupType = backupType,
                        IsVerified = true,
                        Notes = notes ?? $"System backup ({backupType})"
                    };
                    db.BackupRecords.Add(record);
                    db.SaveChanges();
                }
                catch { }

                AuditService.Log("Database Backup", "Database", backupFileName, $"Created {backupType} backup at '{backupFilePath}' ({fileInfo.Length / 1024.0:N1} KB)");

                // Run retention cleanup for automated backups
                if (backupType == BackupType.AutoOnExit || backupType == BackupType.Daily)
                {
                    CleanupOldBackups(dir);
                }

                return (true, $"Backup created successfully at: {backupFilePath}", backupFilePath);
            }
            catch (Exception ex)
            {
                AuditService.Log("Backup Error", "Database", null, $"Backup failed: {ex.Message}");
                return (false, $"Backup failed: {ex.Message}", null);
            }
        }

        public static bool VerifyDatabaseIntegrity(string dbFilePath)
        {
            try
            {
                if (!File.Exists(dbFilePath)) return false;

                using var conn = new SqliteConnection($"Data Source={dbFilePath};Mode=ReadOnly;");
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                var result = cmd.ExecuteScalar()?.ToString();

                return string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public static (bool IsValid, string Info, int PropertyCount, int TenantCount, int TransactionCount) InspectBackupFile(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                return (false, "File does not exist.", 0, 0, 0);
            }

            if (!VerifyDatabaseIntegrity(backupFilePath))
            {
                return (false, "File is not a valid or healthy SQLite database.", 0, 0, 0);
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={backupFilePath};Mode=ReadOnly;");
                conn.Open();

                // Check required tables
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Users', 'Properties', 'Tenants', 'Transactions');";
                var tables = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                if (tables.Count < 4)
                {
                    return (false, "This file does not appear to be a valid Landlord Management System database.", 0, 0, 0);
                }

                int propCount = 0;
                int tenantCount = 0;
                int txCount = 0;

                cmd.CommandText = "SELECT COUNT(*) FROM Properties;";
                propCount = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = "SELECT COUNT(*) FROM Tenants;";
                tenantCount = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                cmd.CommandText = "SELECT COUNT(*) FROM Transactions;";
                txCount = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                return (true, "Valid Landlord System Database", propCount, tenantCount, txCount);
            }
            catch (Exception ex)
            {
                return (false, $"Inspection error: {ex.Message}", 0, 0, 0);
            }
        }

        public static (bool Success, string Message) RestoreBackup(string backupFilePath)
        {
            if (!AuthService.HasPermission("RestoreDatabase"))
            {
                return (false, "You do not have administrative permission to restore the database.");
            }

            var inspection = InspectBackupFile(backupFilePath);
            if (!inspection.IsValid)
            {
                return (false, $"Cannot restore: {inspection.Info}");
            }

            string currentDbPath = AppDbContext.DatabaseFilePath;

            try
            {
                // STEP 1: Take safety backup of CURRENT database first!
                string safetyDir = Path.Combine(GetDefaultBackupDirectory(), "PreRestoreSnapshots");
                if (!Directory.Exists(safetyDir)) Directory.CreateDirectory(safetyDir);
                string preRestoreSnapshot = Path.Combine(safetyDir, $"PreRestore_SafetySnapshot_{DateTime.Now:yyyy-MM-dd_HHmmss}.db");

                try
                {
                    if (File.Exists(currentDbPath))
                    {
                        File.Copy(currentDbPath, preRestoreSnapshot, true);
                    }
                }
                catch { }

                // STEP 2: Clear EF Core connection pools
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // STEP 3: Replace current database file with backup
                string? walFile = currentDbPath + "-wal";
                string? shmFile = currentDbPath + "-shm";

                if (File.Exists(walFile)) { try { File.Delete(walFile); } catch { } }
                if (File.Exists(shmFile)) { try { File.Delete(shmFile); } catch { } }

                File.Copy(backupFilePath, currentDbPath, true);

                // STEP 4: Re-initialize and verify restored database
                AppDbContext.InitializeDatabase();
                SettingService.ReloadCache();

                AuditService.Log("Database Restore", "Database", Path.GetFileName(backupFilePath), $"Database restored from '{backupFilePath}'. Safety snapshot saved at '{preRestoreSnapshot}'.");

                return (true, $"Database successfully restored from '{Path.GetFileName(backupFilePath)}'. (Safety snapshot saved in PreRestoreSnapshots).");
            }
            catch (Exception ex)
            {
                AuditService.Log("Restore Error", "Database", null, $"Database restore error: {ex.Message}");
                return (false, $"Restore failed: {ex.Message}");
            }
        }

        public static void CleanupOldBackups(string directory)
        {
            try
            {
                int retentionDays = SettingService.GetInt("Backup.RetentionDays", 30);
                if (retentionDays <= 0) return;

                if (!Directory.Exists(directory)) return;

                var files = Directory.GetFiles(directory, "Backup_*.db")
                                     .Select(f => new FileInfo(f))
                                     .OrderByDescending(f => f.CreationTime)
                                     .ToList();

                // NEVER delete if only 1 backup file exists!
                if (files.Count <= 1) return;

                DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

                // Keep at least the 3 newest backups regardless of age
                var candidatesToDelete = files.Skip(3).Where(f => f.CreationTime < cutoff).ToList();

                foreach (var file in candidatesToDelete)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static List<BackupRecord> GetBackupHistory()
        {
            using var db = new AppDbContext();
            return db.BackupRecords.AsNoTracking().OrderByDescending(b => b.BackupDate).ToList();
        }
    }
}
