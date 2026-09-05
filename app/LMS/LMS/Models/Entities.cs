using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS.Models
{
    public class Landlord
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FatherOrHusbandName { get; set; }

        [MaxLength(50)]
        public string? CnicOrId { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
    }

    public class Property
    {
        [Key]
        public int Id { get; set; }

        public int LandlordId { get; set; }
        public virtual Landlord? Landlord { get; set; }

        [Required, MaxLength(50)]
        public string PropertyCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PropertyType { get; set; } = "Residential"; // Residential, Commercial, Plaza, Building, House, Shop, Flat

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public PropertyStatus Status { get; set; } = PropertyStatus.Active;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<PropertyUnit> Units { get; set; } = new List<PropertyUnit>();
    }

    public class PropertyUnit
    {
        [Key]
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public virtual Property? Property { get; set; }

        [Required, MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty; // e.g. "Shop 1", "Flat 2B", "Ground Floor"

        [MaxLength(50)]
        public string UnitType { get; set; } = "Portion"; // Shop, Flat, Portion, Room, Floor, Warehouse

        [MaxLength(50)]
        public string? Floor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }

        public UnitStatus Status { get; set; } = UnitStatus.Vacant;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RentAgreement> RentAgreements { get; set; } = new List<RentAgreement>();
    }

    /// <summary>
    /// Tenant represents the generalized Customer / Party model (supporting Tenants, Installment Customers, BNPL clients, Wholesalers/Retailers).
    /// </summary>
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TenantCode { get; set; } = string.Empty; // Customer Code

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FatherOrHusbandName { get; set; }

        [MaxLength(50)]
        public string? CnicOrId { get; set; }

        [Required, MaxLength(50)]
        public string ContactNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? AlternateContact { get; set; }

        [MaxLength(250)]
        public string? PermanentAddress { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(50)]
        public string? EmergencyPhone { get; set; }

        public TenantStatus Status { get; set; } = TenantStatus.Active;
        public CustomerType CustomerType { get; set; } = CustomerType.Tenant;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } = 0;

        // Guarantor / Reference Information
        [MaxLength(150)]
        public string? GuarantorName { get; set; }

        [MaxLength(100)]
        public string? GuarantorFatherName { get; set; }

        [MaxLength(50)]
        public string? GuarantorCnic { get; set; }

        [MaxLength(50)]
        public string? GuarantorPhone { get; set; }

        [MaxLength(250)]
        public string? GuarantorAddress { get; set; }

        [MaxLength(100)]
        public string? GuarantorRelation { get; set; }

        [MaxLength(500)]
        public string? GuarantorNotes { get; set; }

        [MaxLength(50)]
        public string? Rating { get; set; } = "Good"; // Good, Fair, Risky, Defaulter

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RentAgreement> RentAgreements { get; set; } = new List<RentAgreement>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<InstallmentSale> InstallmentSales { get; set; } = new List<InstallmentSale>();
    }

    public class RentAgreement
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string AgreementCode { get; set; } = string.Empty;

        public int TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        public int PropertyUnitId { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceAmount { get; set; }

        public int DueDayOfMonth { get; set; } = 5; // e.g. 1-31

        [Column(TypeName = "decimal(5,2)")]
        public decimal RentIncrementRatePercent { get; set; } = 10; // Annual increment %

        public AgreementStatus Status { get; set; } = AgreementStatus.Active;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RentRateHistory> RentRateHistories { get; set; } = new List<RentRateHistory>();
        public virtual ICollection<RentSchedule> RentSchedules { get; set; } = new List<RentSchedule>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class RentRateHistory
    {
        [Key]
        public int Id { get; set; }

        public int RentAgreementId { get; set; }
        public virtual RentAgreement? RentAgreement { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OldRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewRent { get; set; }

        public DateTime EffectiveDate { get; set; }

        [MaxLength(250)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class RentSchedule
    {
        [Key]
        public int Id { get; set; }

        public int RentAgreementId { get; set; }
        public virtual RentAgreement? RentAgreement { get; set; }

        [Required, MaxLength(7)]
        public string MonthYear { get; set; } = string.Empty; // Format: "YYYY-MM" (e.g. "2026-09")

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UtilityCharges { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaintenanceCharges { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        public RentScheduleStatus Status { get; set; } = RentScheduleStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Product model for retail, installment, and BNPL merchandise.
    /// </summary>
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General"; // Mobile, Electronics, Appliances, Furniture, Motorcycle, General Merchandise

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Model { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSalePrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentSalePrice { get; set; } = 0;

        public int CurrentStock { get; set; } = 0;
        public int MinimumStockLevel { get; set; } = 2;

        [MaxLength(50)]
        public string Unit { get; set; } = "Pcs"; // Pcs, Unit, Box, Set

        [MaxLength(100)]
        public string? Warranty { get; set; } // e.g. "1 Year Official Warranty"

        public bool TrackStock { get; set; } = true;
        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }

    /// <summary>
    /// Complete stock movement ledger tracking all inventory in/out/adjustments.
    /// </summary>
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public DateTime MovementDate { get; set; } = DateTime.Now;
        public StockMovementType MovementType { get; set; } = StockMovementType.Purchase;

        public int Quantity { get; set; } // Positive for in, Negative for out

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [MaxLength(100)]
        public string? Reference { get; set; } // e.g. "PUR-101", "INV-2026-0001", "ADJ-01"

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Master sale entity representing Installment, BNPL, Credit, or Cash sales.
    /// </summary>
    public class InstallmentSale
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty; // e.g. "INV-2026-00001"

        public SaleType SaleType { get; set; } = SaleType.InstallmentSale;

        public int CustomerId { get; set; } // Tenant/Customer
        public virtual Tenant? Customer { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCashPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInstallmentPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalePrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DownPayment { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinancedAmount { get; set; } = 0; // NetSalePrice - DownPayment

        public int NumberOfInstallments { get; set; } = 1;
        public InstallmentFrequency Frequency { get; set; } = InstallmentFrequency.Monthly;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentAmount { get; set; } = 0;

        public DateTime FirstDueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPaid { get; set; } = 0; // DownPayment + installments paid

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        public int GracePeriodDays { get; set; } = 3;

        public InstallmentPlanStatus Status { get; set; } = InstallmentPlanStatus.Active;

        // Guarantor info snapshot at sale time
        [MaxLength(150)]
        public string? GuarantorName { get; set; }

        [MaxLength(50)]
        public string? GuarantorPhone { get; set; }

        [MaxLength(50)]
        public string? GuarantorCnic { get; set; }

        [MaxLength(250)]
        public string? GuarantorAddress { get; set; }

        [MaxLength(1000)]
        public string? TermsAndConditions { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
        public virtual ICollection<InstallmentSchedule> Schedules { get; set; } = new List<InstallmentSchedule>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    /// <summary>
    /// Line items in a sale.
    /// </summary>
    public class SaleItem
    {
        [Key]
        public int Id { get; set; }

        public int InstallmentSaleId { get; set; }
        public virtual InstallmentSale? InstallmentSale { get; set; }

        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Required, MaxLength(200)]
        public string ItemDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } = 0;
    }

    /// <summary>
    /// Installment schedule per sale.
    /// </summary>
    public class InstallmentSchedule
    {
        [Key]
        public int Id { get; set; }

        public int InstallmentSaleId { get; set; }
        public virtual InstallmentSale? InstallmentSale { get; set; }

        public int InstallmentNumber { get; set; } // 1, 2, 3...
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DueAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        public DateTime? PaidDate { get; set; }
        public InstallmentItemStatus Status { get; set; } = InstallmentItemStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Universal Financial Transaction Model.
    /// </summary>
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string TransactionCode { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public TransactionType TransactionType { get; set; }

        public int? RentAgreementId { get; set; }
        public virtual RentAgreement? RentAgreement { get; set; }

        public int? PropertyUnitId { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }

        public int? TenantId { get; set; } // Customer / Party ID
        public virtual Tenant? Tenant { get; set; }

        public int? InstallmentSaleId { get; set; }
        public virtual InstallmentSale? InstallmentSale { get; set; }

        public int? InstallmentScheduleId { get; set; }
        public virtual InstallmentSchedule? InstallmentSchedule { get; set; }

        public int? ProductId { get; set; }
        public virtual Product? Product { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debit { get; set; } = 0; // Charges, rent demanded, invoice amount, expenses

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credit { get; set; } = 0; // Payments received, down payment, deposits, income

        [Column(TypeName = "decimal(18,2)")]
        public decimal RunningBalance { get; set; } = 0;

        public PaymentMethod? PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Cheque no, bank ref, receipt #, invoice #

        [MaxLength(100)]
        public string? BankName { get; set; }

        [Required, MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsVoided { get; set; } = false;

        [MaxLength(250)]
        public string? VoidReason { get; set; }

        public DateTime? VoidDate { get; set; }
        public int? VoidedByUserId { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<PaymentReceipt> PaymentReceipts { get; set; } = new List<PaymentReceipt>();
    }

    /// <summary>
    /// Universal Payment Receipt Model for Rent, Installments, BNPL, and general payments.
    /// </summary>
    public class PaymentReceipt
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty; // e.g. "RCP-2026-00001"

        public int TransactionId { get; set; }
        public virtual Transaction? Transaction { get; set; }

        public int TenantId { get; set; } // Customer / Party ID
        public virtual Tenant? Tenant { get; set; }

        public int? PropertyUnitId { get; set; }
        public virtual PropertyUnit? PropertyUnit { get; set; }

        public int? InstallmentSaleId { get; set; }
        public virtual InstallmentSale? InstallmentSale { get; set; }

        public int? InstallmentScheduleId { get; set; }
        public virtual InstallmentSchedule? InstallmentSchedule { get; set; }

        [MaxLength(50)]
        public string? InvoiceNumber { get; set; }

        public int? InstallmentNumber { get; set; }

        public DateTime PaymentDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? RentalPeriod { get; set; } // e.g. "September 2026" or "Installment #3"

        public DateTime? NextDueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPayment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }

        public int ReceivedByUserId { get; set; }
        public string? ReceivedByUserName { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string PasswordSalt { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Operator;

        public bool IsActive { get; set; } = true;

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public int? UserId { get; set; }

        [MaxLength(50)]
        public string Username { get; set; } = "System";

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EntityId { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(100)]
        public string? MachineName { get; set; }
    }

    public class AppSetting
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = "General"; // General, Rent, Backup, Receipt, Security, Retail, Business

        [MaxLength(250)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class BackupRecord
    {
        [Key]
        public int Id { get; set; }

        public DateTime BackupDate { get; set; } = DateTime.Now;

        [Required, MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public BackupType BackupType { get; set; } = BackupType.Manual;

        public bool IsVerified { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
