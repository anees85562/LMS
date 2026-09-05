using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Models;

namespace LMS.Data
{
    public class AppDbContext : DbContext
    {
        private static string? _customDbPath;

        public static string DatabaseFilePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_customDbPath))
                {
                    return _customDbPath;
                }

                string appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LandlordManagementSystem",
                    "Data"
                );

                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }

                return Path.Combine(appDataDir, "landlord.db");
            }
            set => _customDbPath = value;
        }

        public AppDbContext() : base()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Landlord> Landlords => Set<Landlord>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<PropertyUnit> PropertyUnits => Set<PropertyUnit>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<RentAgreement> RentAgreements => Set<RentAgreement>();
        public DbSet<RentRateHistory> RentRateHistories => Set<RentRateHistory>();
        public DbSet<RentSchedule> RentSchedules => Set<RentSchedule>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<InstallmentSale> InstallmentSales => Set<InstallmentSale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<PaymentReceipt> PaymentReceipts => Set<PaymentReceipt>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<AppSetting> AppSettings => Set<AppSetting>();
        public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath = DatabaseFilePath;
                string dir = Path.GetDirectoryName(dbPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                optionsBuilder.UseSqlite($"Data Source={dbPath};");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
            });

            // Property constraints
            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasIndex(p => p.PropertyCode).IsUnique();
                entity.HasOne(p => p.Landlord)
                      .WithMany(l => l.Properties)
                      .HasForeignKey(p => p.LandlordId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // PropertyUnit constraints
            modelBuilder.Entity<PropertyUnit>(entity =>
            {
                entity.HasIndex(u => new { u.PropertyId, u.UnitNumber }).IsUnique();
                entity.HasOne(u => u.Property)
                      .WithMany(p => p.Units)
                      .HasForeignKey(u => u.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Tenant (Customer/Party) constraints
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasIndex(t => t.TenantCode).IsUnique();
                entity.HasIndex(t => t.ContactNumber);
                entity.HasIndex(t => t.CustomerType);
            });

            // RentAgreement constraints
            modelBuilder.Entity<RentAgreement>(entity =>
            {
                entity.HasIndex(a => a.AgreementCode).IsUnique();
                entity.HasOne(a => a.Tenant)
                      .WithMany(t => t.RentAgreements)
                      .HasForeignKey(a => a.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.PropertyUnit)
                      .WithMany(u => u.RentAgreements)
                      .HasForeignKey(a => a.PropertyUnitId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // RentSchedule constraints
            modelBuilder.Entity<RentSchedule>(entity =>
            {
                entity.HasIndex(s => new { s.RentAgreementId, s.MonthYear }).IsUnique();
                entity.HasOne(s => s.RentAgreement)
                      .WithMany(a => a.RentSchedules)
                      .HasForeignKey(s => s.RentAgreementId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Product constraints
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.ProductCode).IsUnique();
                entity.HasIndex(p => p.Barcode);
                entity.HasIndex(p => p.Category);
            });

            // StockMovement constraints
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasIndex(m => m.MovementDate);
                entity.HasIndex(m => m.MovementType);
                entity.HasOne(m => m.Product)
                      .WithMany(p => p.StockMovements)
                      .HasForeignKey(m => m.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // InstallmentSale constraints
            modelBuilder.Entity<InstallmentSale>(entity =>
            {
                entity.HasIndex(s => s.InvoiceNumber).IsUnique();
                entity.HasIndex(s => s.SaleDate);
                entity.HasIndex(s => s.Status);
                entity.HasOne(s => s.Customer)
                      .WithMany(c => c.InstallmentSales)
                      .HasForeignKey(s => s.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // SaleItem constraints
            modelBuilder.Entity<SaleItem>(entity =>
            {
                entity.HasOne(item => item.InstallmentSale)
                      .WithMany(s => s.Items)
                      .HasForeignKey(item => item.InstallmentSaleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Product)
                      .WithMany(p => p.SaleItems)
                      .HasForeignKey(item => item.ProductId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // InstallmentSchedule constraints
            modelBuilder.Entity<InstallmentSchedule>(entity =>
            {
                entity.HasIndex(s => new { s.InstallmentSaleId, s.InstallmentNumber }).IsUnique();
                entity.HasIndex(s => s.DueDate);
                entity.HasOne(s => s.InstallmentSale)
                      .WithMany(sale => sale.Schedules)
                      .HasForeignKey(s => s.InstallmentSaleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Transaction constraints
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(t => t.TransactionCode).IsUnique();
                entity.HasIndex(t => t.TransactionDate);
                entity.HasIndex(t => t.TransactionType);

                entity.HasOne(t => t.RentAgreement)
                      .WithMany(a => a.Transactions)
                      .HasForeignKey(t => t.RentAgreementId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Tenant)
                      .WithMany(ten => ten.Transactions)
                      .HasForeignKey(t => t.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.InstallmentSale)
                      .WithMany(s => s.Transactions)
                      .HasForeignKey(t => t.InstallmentSaleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // PaymentReceipt constraints
            modelBuilder.Entity<PaymentReceipt>(entity =>
            {
                entity.HasIndex(r => r.ReceiptNumber).IsUnique();
                entity.HasIndex(r => r.PaymentDate);
                entity.HasOne(r => r.Transaction)
                      .WithMany(t => t.PaymentReceipts)
                      .HasForeignKey(r => r.TransactionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AppSetting constraints
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasIndex(s => s.SettingKey).IsUnique();
            });

            // AuditLog constraints
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.Timestamp);
                entity.HasIndex(a => a.Action);
            });
        }

        public static void InitializeDatabase()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();

            // Enable WAL and Foreign Keys on SQLite
            try
            {
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                context.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
            }
            catch
            {
                // Fallback if PRAGMA is restricted
            }

            // Ensure any missing tables, columns, or indexes are created on existing databases
            EnsureSchemaSynchronized(context);

            SeedDefaultSettings(context);
        }

        private static void EnsureSchemaSynchronized(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            bool openedHere = false;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
                openedHere = true;
            }

            try
            {
                // 1. Create all missing tables if they do not exist
                var createTableStatements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Landlords"] = @"CREATE TABLE IF NOT EXISTS Landlords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        FatherOrHusbandName TEXT,
                        CnicOrId TEXT,
                        Phone TEXT,
                        Email TEXT,
                        Address TEXT,
                        Notes TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL
                    );",
                    ["Properties"] = @"CREATE TABLE IF NOT EXISTS Properties (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        LandlordId INTEGER NOT NULL,
                        PropertyCode TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        PropertyType TEXT DEFAULT 'Residential',
                        Address TEXT,
                        City TEXT,
                        Status INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(LandlordId) REFERENCES Landlords(Id) ON DELETE RESTRICT
                    );",
                    ["PropertyUnits"] = @"CREATE TABLE IF NOT EXISTS PropertyUnits (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PropertyId INTEGER NOT NULL,
                        UnitNumber TEXT NOT NULL,
                        UnitType TEXT DEFAULT 'Portion',
                        Floor TEXT,
                        BaseRent REAL NOT NULL DEFAULT 0,
                        Status INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(PropertyId) REFERENCES Properties(Id) ON DELETE RESTRICT
                    );",
                    ["Tenants"] = @"CREATE TABLE IF NOT EXISTS Tenants (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TenantCode TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        FatherOrHusbandName TEXT,
                        CnicOrId TEXT,
                        ContactNumber TEXT NOT NULL,
                        AlternateContact TEXT,
                        PermanentAddress TEXT,
                        City TEXT,
                        EmergencyContactName TEXT,
                        EmergencyPhone TEXT,
                        Status INTEGER NOT NULL DEFAULT 0,
                        CustomerType INTEGER NOT NULL DEFAULT 0,
                        CreditLimit REAL NOT NULL DEFAULT 0,
                        GuarantorName TEXT,
                        GuarantorFatherName TEXT,
                        GuarantorCnic TEXT,
                        GuarantorPhone TEXT,
                        GuarantorAddress TEXT,
                        GuarantorRelation TEXT,
                        GuarantorNotes TEXT,
                        Rating TEXT DEFAULT 'Good',
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL
                    );",
                    ["RentAgreements"] = @"CREATE TABLE IF NOT EXISTS RentAgreements (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AgreementCode TEXT NOT NULL,
                        TenantId INTEGER NOT NULL,
                        PropertyUnitId INTEGER NOT NULL,
                        StartDate TEXT NOT NULL,
                        EndDate TEXT,
                        MonthlyRent REAL NOT NULL DEFAULT 0,
                        SecurityDeposit REAL NOT NULL DEFAULT 0,
                        AdvanceAmount REAL NOT NULL DEFAULT 0,
                        DueDayOfMonth INTEGER NOT NULL DEFAULT 5,
                        RentIncrementRatePercent REAL NOT NULL DEFAULT 10,
                        Status INTEGER NOT NULL DEFAULT 0,
                        Remarks TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(TenantId) REFERENCES Tenants(Id) ON DELETE RESTRICT,
                        FOREIGN KEY(PropertyUnitId) REFERENCES PropertyUnits(Id) ON DELETE RESTRICT
                    );",
                    ["RentRateHistories"] = @"CREATE TABLE IF NOT EXISTS RentRateHistories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RentAgreementId INTEGER NOT NULL,
                        OldRent REAL NOT NULL DEFAULT 0,
                        NewRent REAL NOT NULL DEFAULT 0,
                        EffectiveDate TEXT NOT NULL,
                        Reason TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(RentAgreementId) REFERENCES RentAgreements(Id) ON DELETE RESTRICT
                    );",
                    ["RentSchedules"] = @"CREATE TABLE IF NOT EXISTS RentSchedules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RentAgreementId INTEGER NOT NULL,
                        MonthYear TEXT NOT NULL,
                        DueDate TEXT NOT NULL,
                        BaseRent REAL NOT NULL DEFAULT 0,
                        UtilityCharges REAL NOT NULL DEFAULT 0,
                        MaintenanceCharges REAL NOT NULL DEFAULT 0,
                        LateFee REAL NOT NULL DEFAULT 0,
                        TotalDue REAL NOT NULL DEFAULT 0,
                        AmountPaid REAL NOT NULL DEFAULT 0,
                        Balance REAL NOT NULL DEFAULT 0,
                        Status INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(RentAgreementId) REFERENCES RentAgreements(Id) ON DELETE RESTRICT
                    );",
                    ["Products"] = @"CREATE TABLE IF NOT EXISTS Products (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProductCode TEXT NOT NULL,
                        Barcode TEXT,
                        Name TEXT NOT NULL,
                        Category TEXT DEFAULT 'General',
                        Brand TEXT,
                        Model TEXT,
                        SerialNumber TEXT,
                        PurchasePrice REAL NOT NULL DEFAULT 0,
                        CashSalePrice REAL NOT NULL DEFAULT 0,
                        InstallmentSalePrice REAL NOT NULL DEFAULT 0,
                        CurrentStock INTEGER NOT NULL DEFAULT 0,
                        MinimumStockLevel INTEGER NOT NULL DEFAULT 2,
                        Unit TEXT DEFAULT 'Pcs',
                        Warranty TEXT,
                        TrackStock INTEGER NOT NULL DEFAULT 1,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL
                    );",
                    ["StockMovements"] = @"CREATE TABLE IF NOT EXISTS StockMovements (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProductId INTEGER NOT NULL,
                        MovementDate TEXT NOT NULL,
                        MovementType INTEGER NOT NULL DEFAULT 0,
                        Quantity INTEGER NOT NULL DEFAULT 0,
                        UnitPrice REAL NOT NULL DEFAULT 0,
                        Reference TEXT,
                        Remarks TEXT,
                        UserId INTEGER,
                        Username TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
                    );",
                    ["InstallmentSales"] = @"CREATE TABLE IF NOT EXISTS InstallmentSales (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InvoiceNumber TEXT NOT NULL,
                        SaleType INTEGER NOT NULL DEFAULT 0,
                        CustomerId INTEGER NOT NULL,
                        SaleDate TEXT NOT NULL,
                        TotalCashPrice REAL NOT NULL DEFAULT 0,
                        TotalInstallmentPrice REAL NOT NULL DEFAULT 0,
                        Discount REAL NOT NULL DEFAULT 0,
                        NetSalePrice REAL NOT NULL DEFAULT 0,
                        DownPayment REAL NOT NULL DEFAULT 0,
                        FinancedAmount REAL NOT NULL DEFAULT 0,
                        NumberOfInstallments INTEGER NOT NULL DEFAULT 1,
                        Frequency INTEGER NOT NULL DEFAULT 2,
                        InstallmentAmount REAL NOT NULL DEFAULT 0,
                        FirstDueDate TEXT NOT NULL,
                        TotalPaid REAL NOT NULL DEFAULT 0,
                        RemainingBalance REAL NOT NULL DEFAULT 0,
                        LateFee REAL NOT NULL DEFAULT 0,
                        GracePeriodDays INTEGER NOT NULL DEFAULT 3,
                        Status INTEGER NOT NULL DEFAULT 0,
                        GuarantorName TEXT,
                        GuarantorPhone TEXT,
                        GuarantorCnic TEXT,
                        GuarantorAddress TEXT,
                        TermsAndConditions TEXT,
                        Notes TEXT,
                        CreatedByUserId INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(CustomerId) REFERENCES Tenants(Id) ON DELETE RESTRICT
                    );",
                    ["SaleItems"] = @"CREATE TABLE IF NOT EXISTS SaleItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InstallmentSaleId INTEGER NOT NULL,
                        ProductId INTEGER,
                        ItemDescription TEXT NOT NULL,
                        SerialNumber TEXT,
                        Quantity INTEGER NOT NULL DEFAULT 1,
                        UnitPrice REAL NOT NULL DEFAULT 0,
                        InstallmentPrice REAL NOT NULL DEFAULT 0,
                        TotalPrice REAL NOT NULL DEFAULT 0,
                        FOREIGN KEY(InstallmentSaleId) REFERENCES InstallmentSales(Id) ON DELETE CASCADE,
                        FOREIGN KEY(ProductId) REFERENCES Products(Id) ON DELETE SET NULL
                    );",
                    ["InstallmentSchedules"] = @"CREATE TABLE IF NOT EXISTS InstallmentSchedules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        InstallmentSaleId INTEGER NOT NULL,
                        InstallmentNumber INTEGER NOT NULL,
                        DueDate TEXT NOT NULL,
                        DueAmount REAL NOT NULL DEFAULT 0,
                        PaidAmount REAL NOT NULL DEFAULT 0,
                        RemainingAmount REAL NOT NULL DEFAULT 0,
                        LateFee REAL NOT NULL DEFAULT 0,
                        PaidDate TEXT,
                        Status INTEGER NOT NULL DEFAULT 0,
                        Notes TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(InstallmentSaleId) REFERENCES InstallmentSales(Id) ON DELETE CASCADE
                    );",
                    ["Transactions"] = @"CREATE TABLE IF NOT EXISTS Transactions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TransactionCode TEXT NOT NULL,
                        TransactionDate TEXT NOT NULL,
                        TransactionType INTEGER NOT NULL DEFAULT 0,
                        RentAgreementId INTEGER,
                        PropertyUnitId INTEGER,
                        TenantId INTEGER,
                        InstallmentSaleId INTEGER,
                        InstallmentScheduleId INTEGER,
                        ProductId INTEGER,
                        Debit REAL NOT NULL DEFAULT 0,
                        Credit REAL NOT NULL DEFAULT 0,
                        RunningBalance REAL NOT NULL DEFAULT 0,
                        PaymentMethod INTEGER,
                        ReferenceNumber TEXT,
                        BankName TEXT,
                        Description TEXT NOT NULL,
                        Remarks TEXT,
                        IsVoided INTEGER NOT NULL DEFAULT 0,
                        VoidReason TEXT,
                        VoidDate TEXT,
                        VoidedByUserId INTEGER,
                        CreatedByUserId INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(RentAgreementId) REFERENCES RentAgreements(Id) ON DELETE RESTRICT,
                        FOREIGN KEY(TenantId) REFERENCES Tenants(Id) ON DELETE RESTRICT,
                        FOREIGN KEY(InstallmentSaleId) REFERENCES InstallmentSales(Id) ON DELETE RESTRICT
                    );",
                    ["PaymentReceipts"] = @"CREATE TABLE IF NOT EXISTS PaymentReceipts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReceiptNumber TEXT NOT NULL,
                        TransactionId INTEGER NOT NULL,
                        TenantId INTEGER NOT NULL,
                        PropertyUnitId INTEGER,
                        InstallmentSaleId INTEGER,
                        InstallmentScheduleId INTEGER,
                        InvoiceNumber TEXT,
                        InstallmentNumber INTEGER,
                        PaymentDate TEXT NOT NULL,
                        AmountPaid REAL NOT NULL DEFAULT 0,
                        PaymentMethod INTEGER NOT NULL DEFAULT 0,
                        ReferenceNumber TEXT,
                        BankName TEXT,
                        RentalPeriod TEXT,
                        NextDueDate TEXT,
                        PreviousBalance REAL NOT NULL DEFAULT 0,
                        CurrentPayment REAL NOT NULL DEFAULT 0,
                        RemainingBalance REAL NOT NULL DEFAULT 0,
                        ReceivedByUserId INTEGER NOT NULL DEFAULT 0,
                        ReceivedByUserName TEXT,
                        Remarks TEXT,
                        CreatedAt TEXT NOT NULL,
                        FOREIGN KEY(TransactionId) REFERENCES Transactions(Id) ON DELETE RESTRICT
                    );",
                    ["Users"] = @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        PasswordSalt TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        Role INTEGER NOT NULL DEFAULT 0,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        FailedLoginAttempts INTEGER NOT NULL DEFAULT 0,
                        LockoutEnd TEXT,
                        LastLoginAt TEXT,
                        CreatedAt TEXT NOT NULL
                    );",
                    ["AuditLogs"] = @"CREATE TABLE IF NOT EXISTS AuditLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        UserId INTEGER,
                        Username TEXT,
                        Action TEXT NOT NULL,
                        EntityName TEXT NOT NULL,
                        EntityId TEXT,
                        Details TEXT,
                        MachineName TEXT
                    );",
                    ["AppSettings"] = @"CREATE TABLE IF NOT EXISTS AppSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SettingKey TEXT NOT NULL,
                        SettingValue TEXT NOT NULL,
                        Category TEXT,
                        Description TEXT,
                        UpdatedAt TEXT NOT NULL
                    );",
                    ["BackupRecords"] = @"CREATE TABLE IF NOT EXISTS BackupRecords (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        BackupDate TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        FileSizeBytes INTEGER NOT NULL DEFAULT 0,
                        BackupType INTEGER NOT NULL DEFAULT 0,
                        IsVerified INTEGER NOT NULL DEFAULT 1,
                        Notes TEXT
                    );"
                };

                foreach (var kvp in createTableStatements)
                {
                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = kvp.Value;
                        cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        // Ignore if table already exists or constraint already present
                    }
                }

                // 2. Ensure all columns in every table exist (for any database that existed prior to new columns)
                var tableColumns = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Landlords"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Name"] = "TEXT NOT NULL DEFAULT ''",
                        ["FatherOrHusbandName"] = "TEXT",
                        ["CnicOrId"] = "TEXT",
                        ["Phone"] = "TEXT",
                        ["Email"] = "TEXT",
                        ["Address"] = "TEXT",
                        ["Notes"] = "TEXT",
                        ["IsActive"] = "INTEGER NOT NULL DEFAULT 1",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["Properties"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["LandlordId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["PropertyCode"] = "TEXT NOT NULL DEFAULT ''",
                        ["Name"] = "TEXT NOT NULL DEFAULT ''",
                        ["PropertyType"] = "TEXT DEFAULT 'Residential'",
                        ["Address"] = "TEXT",
                        ["City"] = "TEXT",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["PropertyUnits"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["PropertyId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["UnitNumber"] = "TEXT NOT NULL DEFAULT ''",
                        ["UnitType"] = "TEXT DEFAULT 'Portion'",
                        ["Floor"] = "TEXT",
                        ["BaseRent"] = "REAL NOT NULL DEFAULT 0",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["Tenants"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["TenantCode"] = "TEXT NOT NULL DEFAULT ''",
                        ["FullName"] = "TEXT NOT NULL DEFAULT ''",
                        ["FatherOrHusbandName"] = "TEXT",
                        ["CnicOrId"] = "TEXT",
                        ["ContactNumber"] = "TEXT NOT NULL DEFAULT ''",
                        ["AlternateContact"] = "TEXT",
                        ["PermanentAddress"] = "TEXT",
                        ["City"] = "TEXT",
                        ["EmergencyContactName"] = "TEXT",
                        ["EmergencyPhone"] = "TEXT",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["CustomerType"] = "INTEGER NOT NULL DEFAULT 0",
                        ["CreditLimit"] = "REAL NOT NULL DEFAULT 0",
                        ["GuarantorName"] = "TEXT",
                        ["GuarantorFatherName"] = "TEXT",
                        ["GuarantorCnic"] = "TEXT",
                        ["GuarantorPhone"] = "TEXT",
                        ["GuarantorAddress"] = "TEXT",
                        ["GuarantorRelation"] = "TEXT",
                        ["GuarantorNotes"] = "TEXT",
                        ["Rating"] = "TEXT DEFAULT 'Good'",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["RentAgreements"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["AgreementCode"] = "TEXT NOT NULL DEFAULT ''",
                        ["TenantId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["PropertyUnitId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["StartDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["EndDate"] = "TEXT",
                        ["MonthlyRent"] = "REAL NOT NULL DEFAULT 0",
                        ["SecurityDeposit"] = "REAL NOT NULL DEFAULT 0",
                        ["AdvanceAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["DueDayOfMonth"] = "INTEGER NOT NULL DEFAULT 5",
                        ["RentIncrementRatePercent"] = "REAL NOT NULL DEFAULT 10",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Remarks"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["RentRateHistories"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["RentAgreementId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["OldRent"] = "REAL NOT NULL DEFAULT 0",
                        ["NewRent"] = "REAL NOT NULL DEFAULT 0",
                        ["EffectiveDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["Reason"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["RentSchedules"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["RentAgreementId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["MonthYear"] = "TEXT NOT NULL DEFAULT ''",
                        ["DueDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["BaseRent"] = "REAL NOT NULL DEFAULT 0",
                        ["UtilityCharges"] = "REAL NOT NULL DEFAULT 0",
                        ["MaintenanceCharges"] = "REAL NOT NULL DEFAULT 0",
                        ["LateFee"] = "REAL NOT NULL DEFAULT 0",
                        ["TotalDue"] = "REAL NOT NULL DEFAULT 0",
                        ["AmountPaid"] = "REAL NOT NULL DEFAULT 0",
                        ["Balance"] = "REAL NOT NULL DEFAULT 0",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["Products"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ProductCode"] = "TEXT NOT NULL DEFAULT ''",
                        ["Barcode"] = "TEXT",
                        ["Name"] = "TEXT NOT NULL DEFAULT ''",
                        ["Category"] = "TEXT DEFAULT 'General'",
                        ["Brand"] = "TEXT",
                        ["Model"] = "TEXT",
                        ["SerialNumber"] = "TEXT",
                        ["PurchasePrice"] = "REAL NOT NULL DEFAULT 0",
                        ["CashSalePrice"] = "REAL NOT NULL DEFAULT 0",
                        ["InstallmentSalePrice"] = "REAL NOT NULL DEFAULT 0",
                        ["CurrentStock"] = "INTEGER NOT NULL DEFAULT 0",
                        ["MinimumStockLevel"] = "INTEGER NOT NULL DEFAULT 2",
                        ["Unit"] = "TEXT DEFAULT 'Pcs'",
                        ["Warranty"] = "TEXT",
                        ["TrackStock"] = "INTEGER NOT NULL DEFAULT 1",
                        ["IsActive"] = "INTEGER NOT NULL DEFAULT 1",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["StockMovements"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ProductId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["MovementDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["MovementType"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Quantity"] = "INTEGER NOT NULL DEFAULT 0",
                        ["UnitPrice"] = "REAL NOT NULL DEFAULT 0",
                        ["Reference"] = "TEXT",
                        ["Remarks"] = "TEXT",
                        ["UserId"] = "INTEGER",
                        ["Username"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["InstallmentSales"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["InvoiceNumber"] = "TEXT NOT NULL DEFAULT ''",
                        ["SaleType"] = "INTEGER NOT NULL DEFAULT 0",
                        ["CustomerId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["SaleDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["TotalCashPrice"] = "REAL NOT NULL DEFAULT 0",
                        ["TotalInstallmentPrice"] = "REAL NOT NULL DEFAULT 0",
                        ["Discount"] = "REAL NOT NULL DEFAULT 0",
                        ["NetSalePrice"] = "REAL NOT NULL DEFAULT 0",
                        ["DownPayment"] = "REAL NOT NULL DEFAULT 0",
                        ["FinancedAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["NumberOfInstallments"] = "INTEGER NOT NULL DEFAULT 1",
                        ["Frequency"] = "INTEGER NOT NULL DEFAULT 2",
                        ["InstallmentAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["FirstDueDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["TotalPaid"] = "REAL NOT NULL DEFAULT 0",
                        ["RemainingBalance"] = "REAL NOT NULL DEFAULT 0",
                        ["LateFee"] = "REAL NOT NULL DEFAULT 0",
                        ["GracePeriodDays"] = "INTEGER NOT NULL DEFAULT 3",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["GuarantorName"] = "TEXT",
                        ["GuarantorPhone"] = "TEXT",
                        ["GuarantorCnic"] = "TEXT",
                        ["GuarantorAddress"] = "TEXT",
                        ["TermsAndConditions"] = "TEXT",
                        ["Notes"] = "TEXT",
                        ["CreatedByUserId"] = "INTEGER NOT NULL DEFAULT 0",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["SaleItems"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["InstallmentSaleId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["ProductId"] = "INTEGER",
                        ["ItemDescription"] = "TEXT NOT NULL DEFAULT ''",
                        ["SerialNumber"] = "TEXT",
                        ["Quantity"] = "INTEGER NOT NULL DEFAULT 1",
                        ["UnitPrice"] = "REAL NOT NULL DEFAULT 0",
                        ["InstallmentPrice"] = "REAL NOT NULL DEFAULT 0",
                        ["TotalPrice"] = "REAL NOT NULL DEFAULT 0"
                    },
                    ["InstallmentSchedules"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["InstallmentSaleId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["InstallmentNumber"] = "INTEGER NOT NULL DEFAULT 1",
                        ["DueDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["DueAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["PaidAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["RemainingAmount"] = "REAL NOT NULL DEFAULT 0",
                        ["LateFee"] = "REAL NOT NULL DEFAULT 0",
                        ["PaidDate"] = "TEXT",
                        ["Status"] = "INTEGER NOT NULL DEFAULT 0",
                        ["Notes"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["Transactions"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["TransactionCode"] = "TEXT NOT NULL DEFAULT ''",
                        ["TransactionDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["TransactionType"] = "INTEGER NOT NULL DEFAULT 0",
                        ["RentAgreementId"] = "INTEGER",
                        ["PropertyUnitId"] = "INTEGER",
                        ["TenantId"] = "INTEGER",
                        ["InstallmentSaleId"] = "INTEGER",
                        ["InstallmentScheduleId"] = "INTEGER",
                        ["ProductId"] = "INTEGER",
                        ["Debit"] = "REAL NOT NULL DEFAULT 0",
                        ["Credit"] = "REAL NOT NULL DEFAULT 0",
                        ["RunningBalance"] = "REAL NOT NULL DEFAULT 0",
                        ["PaymentMethod"] = "INTEGER",
                        ["ReferenceNumber"] = "TEXT",
                        ["BankName"] = "TEXT",
                        ["Description"] = "TEXT NOT NULL DEFAULT ''",
                        ["Remarks"] = "TEXT",
                        ["IsVoided"] = "INTEGER NOT NULL DEFAULT 0",
                        ["VoidReason"] = "TEXT",
                        ["VoidDate"] = "TEXT",
                        ["VoidedByUserId"] = "INTEGER",
                        ["CreatedByUserId"] = "INTEGER NOT NULL DEFAULT 0",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["PaymentReceipts"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ReceiptNumber"] = "TEXT NOT NULL DEFAULT ''",
                        ["TransactionId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["TenantId"] = "INTEGER NOT NULL DEFAULT 1",
                        ["PropertyUnitId"] = "INTEGER",
                        ["InstallmentSaleId"] = "INTEGER",
                        ["InstallmentScheduleId"] = "INTEGER",
                        ["InvoiceNumber"] = "TEXT",
                        ["InstallmentNumber"] = "INTEGER",
                        ["PaymentDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["AmountPaid"] = "REAL NOT NULL DEFAULT 0",
                        ["PaymentMethod"] = "INTEGER NOT NULL DEFAULT 0",
                        ["ReferenceNumber"] = "TEXT",
                        ["BankName"] = "TEXT",
                        ["RentalPeriod"] = "TEXT",
                        ["NextDueDate"] = "TEXT",
                        ["PreviousBalance"] = "REAL NOT NULL DEFAULT 0",
                        ["CurrentPayment"] = "REAL NOT NULL DEFAULT 0",
                        ["RemainingBalance"] = "REAL NOT NULL DEFAULT 0",
                        ["ReceivedByUserId"] = "INTEGER NOT NULL DEFAULT 0",
                        ["ReceivedByUserName"] = "TEXT",
                        ["Remarks"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["Users"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Username"] = "TEXT NOT NULL DEFAULT ''",
                        ["PasswordHash"] = "TEXT NOT NULL DEFAULT ''",
                        ["PasswordSalt"] = "TEXT NOT NULL DEFAULT ''",
                        ["FullName"] = "TEXT NOT NULL DEFAULT ''",
                        ["Role"] = "INTEGER NOT NULL DEFAULT 0",
                        ["IsActive"] = "INTEGER NOT NULL DEFAULT 1",
                        ["FailedLoginAttempts"] = "INTEGER NOT NULL DEFAULT 0",
                        ["LockoutEnd"] = "TEXT",
                        ["LastLoginAt"] = "TEXT",
                        ["CreatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["AuditLogs"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Timestamp"] = "TEXT NOT NULL DEFAULT ''",
                        ["UserId"] = "INTEGER",
                        ["Username"] = "TEXT DEFAULT 'System'",
                        ["Action"] = "TEXT NOT NULL DEFAULT ''",
                        ["EntityName"] = "TEXT NOT NULL DEFAULT ''",
                        ["EntityId"] = "TEXT",
                        ["Details"] = "TEXT",
                        ["MachineName"] = "TEXT"
                    },
                    ["AppSettings"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["SettingKey"] = "TEXT NOT NULL DEFAULT ''",
                        ["SettingValue"] = "TEXT NOT NULL DEFAULT ''",
                        ["Category"] = "TEXT DEFAULT 'General'",
                        ["Description"] = "TEXT",
                        ["UpdatedAt"] = "TEXT NOT NULL DEFAULT ''"
                    },
                    ["BackupRecords"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["BackupDate"] = "TEXT NOT NULL DEFAULT ''",
                        ["FilePath"] = "TEXT NOT NULL DEFAULT ''",
                        ["FileSizeBytes"] = "INTEGER NOT NULL DEFAULT 0",
                        ["BackupType"] = "INTEGER NOT NULL DEFAULT 0",
                        ["IsVerified"] = "INTEGER NOT NULL DEFAULT 1",
                        ["Notes"] = "TEXT"
                    }
                };

                foreach (var (tableName, cols) in tableColumns)
                {
                    var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            existingCols.Add(reader.GetString(1));
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var (colName, colDef) in cols)
                    {
                        if (!existingCols.Contains(colName))
                        {
                            try
                            {
                                using var alterCmd = connection.CreateCommand();
                                alterCmd.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{colName}\" {colDef};";
                                alterCmd.ExecuteNonQuery();
                            }
                            catch
                            {
                                // Column might already exist
                            }
                        }
                    }
                }

                // 3. Create any missing performance indexes
                string[] indexes = new[]
                {
                    "CREATE INDEX IF NOT EXISTS IX_Transactions_InstallmentSaleId ON Transactions (InstallmentSaleId);",
                    "CREATE INDEX IF NOT EXISTS IX_Transactions_TenantId ON Transactions (TenantId);",
                    "CREATE INDEX IF NOT EXISTS IX_InstallmentSales_CustomerId ON InstallmentSales (CustomerId);",
                    "CREATE INDEX IF NOT EXISTS IX_InstallmentSchedules_InstallmentSaleId ON InstallmentSchedules (InstallmentSaleId);",
                    "CREATE INDEX IF NOT EXISTS IX_SaleItems_InstallmentSaleId ON SaleItems (InstallmentSaleId);",
                    "CREATE INDEX IF NOT EXISTS IX_Tenants_CustomerType ON Tenants (CustomerType);"
                };

                foreach (var idx in indexes)
                {
                    try
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = idx;
                        cmd.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
            finally
            {
                if (openedHere && connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        private static void SeedDefaultSettings(AppDbContext context)
        {
            var defaultSettings = new[]
            {
                new AppSetting { SettingKey = "Business.Type", SettingValue = "Mixed", Category = "Business", Description = "Business Mode (PropertyRent, InstallmentRetail, BNPL, GeneralReceivables, Mixed)" },
                new AppSetting { SettingKey = "General.CompanyName", SettingValue = "Easy Installment & Receivables Management", Category = "General", Description = "Business / Company Name" },
                new AppSetting { SettingKey = "General.OwnerName", SettingValue = "Business Owner", Category = "General", Description = "Owner Name" },
                new AppSetting { SettingKey = "General.Address", SettingValue = "Main Commercial Plaza, City", Category = "General", Description = "Office Address" },
                new AppSetting { SettingKey = "General.Phone", SettingValue = "+92 300 0000000", Category = "General", Description = "Contact Phone" },
                new AppSetting { SettingKey = "General.Currency", SettingValue = "Rs.", Category = "General", Description = "Currency Symbol" },
                new AppSetting { SettingKey = "General.DateFormat", SettingValue = "dd/MM/yyyy", Category = "General", Description = "Standard Date Format" },
                new AppSetting { SettingKey = "Rent.DefaultDueDay", SettingValue = "5", Category = "Rent", Description = "Default Due Day of Month" },
                new AppSetting { SettingKey = "Rent.ReminderDaysBefore", SettingValue = "3", Category = "Rent", Description = "Days before due date to alert" },
                new AppSetting { SettingKey = "Rent.OverdueDaysAfter", SettingValue = "1", Category = "Rent", Description = "Days after due date to mark overdue" },
                new AppSetting { SettingKey = "Rent.AgreementExpiryReminderDays", SettingValue = "30", Category = "Rent", Description = "Days before lease expiry to alert" },
                new AppSetting { SettingKey = "Retail.InvoicePrefix", SettingValue = "INV", Category = "Retail", Description = "Sale Invoice Prefix" },
                new AppSetting { SettingKey = "Retail.ProductCodePrefix", SettingValue = "PRD", Category = "Retail", Description = "Product Code Prefix" },
                new AppSetting { SettingKey = "Retail.DefaultGracePeriodDays", SettingValue = "3", Category = "Retail", Description = "Installment Grace Period Days" },
                new AppSetting { SettingKey = "Retail.DefaultLateFeePercent", SettingValue = "0", Category = "Retail", Description = "Late Fee Percentage" },
                new AppSetting { SettingKey = "Backup.DefaultDirectory", SettingValue = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandlordManagementSystem", "Backups"), Category = "Backup", Description = "Default Backup Directory" },
                new AppSetting { SettingKey = "Backup.AutoBackupOnExit", SettingValue = "true", Category = "Backup", Description = "Auto backup on application close" },
                new AppSetting { SettingKey = "Backup.RetentionDays", SettingValue = "30", Category = "Backup", Description = "Days to retain automated backups" },
                new AppSetting { SettingKey = "Receipt.Prefix", SettingValue = "RCP", Category = "Receipt", Description = "Receipt Number Prefix" },
                new AppSetting { SettingKey = "Receipt.HeaderNote", SettingValue = "Official Payment & Account Receipt", Category = "Receipt", Description = "Receipt Title Header" },
                new AppSetting { SettingKey = "Receipt.FooterNote", SettingValue = "Thank you for your timely payment. Computer generated receipt.", Category = "Receipt", Description = "Receipt Footer Message" },
                new AppSetting { SettingKey = "Security.AutoLockMinutes", SettingValue = "15", Category = "Security", Description = "Auto-lock after minutes of inactivity (0 to disable)" }
            };

            bool changed = false;
            foreach (var setting in defaultSettings)
            {
                if (!context.AppSettings.Any(s => s.SettingKey == setting.SettingKey))
                {
                    context.AppSettings.Add(setting);
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }
    }
}
