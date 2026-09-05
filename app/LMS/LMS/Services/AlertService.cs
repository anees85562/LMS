using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class SystemAlert
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // RentDue, InstallmentDue, Overdue, Expiry, Vacancy, LowStock, CreditLimit, Backup
        public string Severity { get; set; } = "Info"; // Info, Warning, Danger, Success
        public string? ActionKey { get; set; }
        public int? ReferenceId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class AlertService
    {
        public static List<SystemAlert> GenerateActiveAlerts()
        {
            var alerts = new List<SystemAlert>();
            DateTime today = DateTime.Now.Date;

            int reminderDaysBefore = SettingService.GetInt("Rent.ReminderDaysBefore", 3);
            int overdueDaysAfter = SettingService.GetInt("Rent.OverdueDaysAfter", 1);
            int expiryDays = SettingService.GetInt("Rent.AgreementExpiryReminderDays", 30);

            using var db = new AppDbContext();

            // 1. Rent Due Today & Upcoming Rent
            string currentMonthYear = today.ToString("yyyy-MM");
            var currentRentSchedules = db.RentSchedules
                                         .Include(s => s.RentAgreement)
                                         .ThenInclude(a => a!.Tenant)
                                         .Include(s => s.RentAgreement)
                                         .ThenInclude(a => a!.PropertyUnit)
                                         .Where(s => s.MonthYear == currentMonthYear && s.Balance > 0)
                                         .ToList();

            foreach (var s in currentRentSchedules)
            {
                var due = s.DueDate.Date;
                var tenantName = s.RentAgreement?.Tenant?.FullName ?? "Tenant";
                var unit = s.RentAgreement?.PropertyUnit?.UnitNumber ?? "Unit";

                if (due == today)
                {
                    alerts.Add(new SystemAlert
                    {
                        Title = "Rent Due Today",
                        Message = $"Rent of {SettingService.FormatCurrency(s.Balance)} for {tenantName} ({unit}) is due today.",
                        Category = "RentDue",
                        Severity = "Warning",
                        ActionKey = "Payment",
                        ReferenceId = s.RentAgreement?.TenantId
                    });
                }
                else if (due > today && (due - today).TotalDays <= reminderDaysBefore)
                {
                    int daysLeft = (int)(due - today).TotalDays;
                    alerts.Add(new SystemAlert
                    {
                        Title = "Upcoming Rent Due",
                        Message = $"Rent of {SettingService.FormatCurrency(s.Balance)} for {tenantName} ({unit}) is due in {daysLeft} day(s).",
                        Category = "RentDue",
                        Severity = "Info",
                        ActionKey = "Payment",
                        ReferenceId = s.RentAgreement?.TenantId
                    });
                }
            }

            // 2. Overdue Rent Alerts
            var overdueRentSchedules = db.RentSchedules
                                         .Include(s => s.RentAgreement)
                                         .ThenInclude(a => a!.Tenant)
                                         .Include(s => s.RentAgreement)
                                         .ThenInclude(a => a!.PropertyUnit)
                                         .Where(s => s.Balance > 0 && s.DueDate.Date < today.AddDays(-overdueDaysAfter + 1))
                                         .ToList();

            var overdueByTenant = overdueRentSchedules.GroupBy(s => s.RentAgreement?.TenantId ?? 0);
            foreach (var group in overdueByTenant)
            {
                var first = group.First();
                var tenantName = first.RentAgreement?.Tenant?.FullName ?? "Tenant";
                var unit = first.RentAgreement?.PropertyUnit?.UnitNumber ?? "Unit";
                decimal totalOverdue = group.Sum(s => s.Balance);
                int monthsOverdue = group.Count();

                alerts.Add(new SystemAlert
                {
                    Title = "Overdue Rent Notice",
                    Message = $"{tenantName} ({unit}) has overdue rent of {SettingService.FormatCurrency(totalOverdue)} ({monthsOverdue} month(s) pending).",
                    Category = "Overdue",
                    Severity = "Danger",
                    ActionKey = "Payment",
                    ReferenceId = first.RentAgreement?.TenantId
                });
            }

            // 3. Installment Due Today & Upcoming Installments
            var currentInstSchedules = db.InstallmentSchedules
                .Include(s => s.InstallmentSale).ThenInclude(sale => sale!.Customer)
                .Where(s => s.Status != InstallmentItemStatus.Paid && s.RemainingAmount > 0)
                .ToList();

            foreach (var sch in currentInstSchedules)
            {
                var due = sch.DueDate.Date;
                var custName = sch.InstallmentSale?.Customer?.FullName ?? "Customer";
                var invNum = sch.InstallmentSale?.InvoiceNumber ?? "";

                if (due == today)
                {
                    alerts.Add(new SystemAlert
                    {
                        Title = "Installment Due Today",
                        Message = $"Installment #{sch.InstallmentNumber} of {SettingService.FormatCurrency(sch.RemainingAmount)} for {custName} (Inv: {invNum}) is due today.",
                        Category = "InstallmentDue",
                        Severity = "Warning",
                        ActionKey = "InstallmentPayment",
                        ReferenceId = sch.InstallmentSaleId
                    });
                }
                else if (due > today && (due - today).TotalDays <= reminderDaysBefore)
                {
                    int daysLeft = (int)(due - today).TotalDays;
                    alerts.Add(new SystemAlert
                    {
                        Title = "Upcoming Installment Due",
                        Message = $"Installment #{sch.InstallmentNumber} of {SettingService.FormatCurrency(sch.RemainingAmount)} for {custName} (Inv: {invNum}) is due in {daysLeft} day(s).",
                        Category = "InstallmentDue",
                        Severity = "Info",
                        ActionKey = "InstallmentPayment",
                        ReferenceId = sch.InstallmentSaleId
                    });
                }
                else if (due < today)
                {
                    int daysLate = (int)(today - due).TotalDays;
                    alerts.Add(new SystemAlert
                    {
                        Title = "Overdue Installment",
                        Message = $"Installment #{sch.InstallmentNumber} for {custName} (Inv: {invNum}) is {daysLate} day(s) overdue! Amount: {SettingService.FormatCurrency(sch.RemainingAmount)}",
                        Category = "Overdue",
                        Severity = "Danger",
                        ActionKey = "InstallmentPayment",
                        ReferenceId = sch.InstallmentSaleId
                    });
                }
            }

            // 4. Low Stock Products
            var lowStockProducts = db.Products
                .Where(p => p.IsActive && p.TrackStock && p.CurrentStock <= p.MinimumStockLevel)
                .ToList();

            foreach (var p in lowStockProducts)
            {
                alerts.Add(new SystemAlert
                {
                    Title = "Low Stock Alert",
                    Message = $"Product '{p.Name}' ({p.ProductCode}) has low stock: {p.CurrentStock} {p.Unit} remaining (Min: {p.MinimumStockLevel}).",
                    Category = "LowStock",
                    Severity = p.CurrentStock == 0 ? "Danger" : "Warning",
                    ActionKey = "Products",
                    ReferenceId = p.Id
                });
            }

            // 5. Customer Credit Limit Exceeded
            var customersWithLimit = db.Tenants
                .Include(t => t.Transactions)
                .Where(t => t.Status == TenantStatus.Active && t.CreditLimit > 0)
                .ToList();

            foreach (var cust in customersWithLimit)
            {
                var validTx = cust.Transactions.Where(t => !t.IsVoided).ToList();
                decimal outstanding = validTx.Sum(t => t.Debit) - validTx.Sum(t => t.Credit);
                if (outstanding > cust.CreditLimit)
                {
                    alerts.Add(new SystemAlert
                    {
                        Title = "Credit Limit Exceeded",
                        Message = $"Customer '{cust.FullName}' outstanding ({SettingService.FormatCurrency(outstanding)}) exceeds credit limit of {SettingService.FormatCurrency(cust.CreditLimit)}.",
                        Category = "CreditLimit",
                        Severity = "Danger",
                        ActionKey = "CustomerProfile",
                        ReferenceId = cust.Id
                    });
                }
            }

            // 6. Lease Agreement Expiry
            DateTime expiryWindowEnd = today.AddDays(expiryDays);
            var expiringAgreements = db.RentAgreements
                                       .Include(a => a.Tenant)
                                       .Include(a => a.PropertyUnit)
                                       .Where(a => a.Status == AgreementStatus.Active &&
                                                   a.EndDate.HasValue &&
                                                   a.EndDate.Value.Date >= today &&
                                                   a.EndDate.Value.Date <= expiryWindowEnd)
                                       .ToList();

            foreach (var agr in expiringAgreements)
            {
                int daysRemaining = (int)(agr.EndDate!.Value.Date - today).TotalDays;
                var tenantName = agr.Tenant?.FullName ?? "Tenant";
                var unit = agr.PropertyUnit?.UnitNumber ?? "Unit";

                alerts.Add(new SystemAlert
                {
                    Title = "Lease Agreement Expiring",
                    Message = $"Agreement for {tenantName} ({unit}) will expire in {daysRemaining} day(s) on {agr.EndDate.Value:dd/MM/yyyy}.",
                    Category = "Expiry",
                    Severity = "Warning",
                    ActionKey = "Agreement",
                    ReferenceId = agr.Id
                });
            }

            // 7. Vacant Units
            int vacantCount = db.PropertyUnits.Count(u => u.Status == UnitStatus.Vacant);
            if (vacantCount > 0)
            {
                alerts.Add(new SystemAlert
                {
                    Title = "Vacant Properties Available",
                    Message = $"{vacantCount} property unit(s) are currently vacant and available for rent.",
                    Category = "Vacancy",
                    Severity = "Info",
                    ActionKey = "Properties",
                    ReferenceId = null
                });
            }

            // 8. Database Backup Status
            var lastBackup = db.BackupRecords.OrderByDescending(b => b.BackupDate).FirstOrDefault();
            if (lastBackup == null)
            {
                alerts.Add(new SystemAlert
                {
                    Title = "No Database Backup Found",
                    Message = "No backup has been created yet. Please perform a database backup to prevent data loss.",
                    Category = "Backup",
                    Severity = "Danger",
                    ActionKey = "Backup",
                    ReferenceId = null
                });
            }
            else
            {
                int daysSinceBackup = (int)(today - lastBackup.BackupDate.Date).TotalDays;
                if (daysSinceBackup >= 7)
                {
                    alerts.Add(new SystemAlert
                    {
                        Title = "Database Backup Overdue",
                        Message = $"Last database backup was created {daysSinceBackup} days ago on {lastBackup.BackupDate:dd/MM/yyyy}.",
                        Category = "Backup",
                        Severity = "Warning",
                        ActionKey = "Backup",
                        ReferenceId = null
                    });
                }
            }

            return alerts;
        }
    }
}
