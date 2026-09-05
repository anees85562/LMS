using System;

namespace LMS.Models
{
    public enum BusinessType
    {
        PropertyRent = 1,
        InstallmentRetail = 2,
        BNPL = 3,
        GeneralReceivables = 4,
        Mixed = 5
    }

    public enum CustomerType
    {
        Tenant = 1,
        InstallmentCustomer = 2,
        BNPLCreditCustomer = 3,
        GeneralParty = 4
    }

    public enum UserRole
    {
        Administrator = 1,
        Operator = 2,
        Viewer = 3
    }

    public enum PropertyStatus
    {
        Active = 1,
        Inactive = 2,
        Archived = 3
    }

    public enum UnitStatus
    {
        Vacant = 1,
        Occupied = 2,
        UnderMaintenance = 3
    }

    public enum TenantStatus
    {
        Active = 1,
        Previous = 2,
        Evicted = 3,
        Blacklisted = 4
    }

    public enum AgreementStatus
    {
        Active = 1,
        Expired = 2,
        Terminated = 3,
        Renewed = 4
    }

    public enum RentScheduleStatus
    {
        Pending = 1,
        Partial = 2,
        Paid = 3,
        Overdue = 4
    }

    public enum SaleType
    {
        InstallmentSale = 1,
        BNPLSale = 2,
        CreditSale = 3,
        CashSale = 4
    }

    public enum InstallmentFrequency
    {
        Monthly = 1,
        Weekly = 2,
        BiWeekly = 3,
        Custom = 4
    }

    public enum InstallmentPlanStatus
    {
        Active = 1,
        PartiallyPaid = 2,
        Completed = 3,
        Overdue = 4,
        Defaulted = 5,
        Cancelled = 6,
        Settled = 7
    }

    public enum InstallmentItemStatus
    {
        Pending = 1,
        Partial = 2,
        Paid = 3,
        Overdue = 4,
        Cancelled = 5
    }

    public enum StockMovementType
    {
        OpeningStock = 1,
        Purchase = 2,
        Sale = 3,
        Return = 4,
        StockAdjustment = 5,
        DamagedStock = 6
    }

    public enum ReceivableSourceType
    {
        PropertyRent = 1,
        InstallmentSale = 2,
        BNPLSale = 3,
        CreditSale = 4,
        MaintenanceCharge = 5,
        ServiceCharge = 6,
        Other = 7
    }

    public enum ReceivableStatus
    {
        Active = 1,
        PartiallyPaid = 2,
        Paid = 3,
        Overdue = 4,
        Defaulted = 5,
        Cancelled = 6,
        Settled = 7
    }

    public enum TransactionType
    {
        MonthlyRentCharge = 1,
        RentPayment = 2,
        AdvanceRent = 3,
        SecurityDeposit = 4,
        UtilityBill = 5,
        MaintenanceExpense = 6,
        RepairExpense = 7,
        OtherIncome = 8,
        OtherExpense = 9,
        Adjustment = 10,
        PenaltyDiscount = 11,
        SecurityRefund = 12,

        // Retail & Installment & BNPL Transactions
        SaleInvoice = 20,
        DownPayment = 21,
        InstallmentPayment = 22,
        BNPLCharge = 23,
        BNPLPayment = 24,
        CreditSale = 25,
        CreditPayment = 26,
        StockPurchase = 27,
        SaleReturn = 28,
        LateFeeCharge = 29,
        EarlySettlementDiscount = 30
    }

    public enum PaymentMethod
    {
        Cash = 1,
        BankTransfer = 2,
        Cheque = 3,
        OnlineTransfer = 4,
        Other = 5
    }

    public enum BackupType
    {
        Manual = 1,
        AutoOnExit = 2,
        Daily = 3,
        PreRestore = 4,
        PreImport = 5
    }
}
