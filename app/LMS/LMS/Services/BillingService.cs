using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class BillingService
    {
        public static (int GeneratedCount, int AlreadyExistingCount, decimal TotalDemanded) GenerateMonthlyRent(int year, int month)
        {
            string monthYear = $"{year:D4}-{month:D2}";
            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            int generated = 0;
            int existing = 0;
            decimal totalDemanded = 0;

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var activeAgreements = db.RentAgreements
                                         .Include(a => a.PropertyUnit)
                                         .ThenInclude(u => u!.Property)
                                         .Include(a => a.Tenant)
                                         .Where(a => a.Status == AgreementStatus.Active &&
                                                     a.StartDate <= monthEnd &&
                                                     (!a.EndDate.HasValue || a.EndDate.Value >= monthStart))
                                         .ToList();

                int userId = AuthService.CurrentUser?.Id ?? 1;

                foreach (var agreement in activeAgreements)
                {
                    // Check if already generated for this agreement and month
                    bool scheduleExists = db.RentSchedules.Any(s => s.RentAgreementId == agreement.Id && s.MonthYear == monthYear);
                    if (scheduleExists)
                    {
                        existing++;
                        continue;
                    }

                    int dueDay = Math.Clamp(agreement.DueDayOfMonth, 1, DateTime.DaysInMonth(year, month));
                    DateTime dueDate = new DateTime(year, month, dueDay);

                    var schedule = new RentSchedule
                    {
                        RentAgreementId = agreement.Id,
                        MonthYear = monthYear,
                        DueDate = dueDate,
                        BaseRent = agreement.MonthlyRent,
                        UtilityCharges = 0,
                        MaintenanceCharges = 0,
                        LateFee = 0,
                        TotalDue = agreement.MonthlyRent,
                        AmountPaid = 0,
                        Balance = agreement.MonthlyRent,
                        Status = (DateTime.Now.Date > dueDate.Date) ? RentScheduleStatus.Overdue : RentScheduleStatus.Pending,
                        CreatedAt = DateTime.Now
                    };

                    db.RentSchedules.Add(schedule);
                    db.SaveChanges(); // Save to obtain schedule.Id

                    // Post Debit Transaction to Ledger
                    string unitName = agreement.PropertyUnit?.UnitNumber ?? "Unit";
                    string propName = agreement.PropertyUnit?.Property?.Name ?? "Property";
                    var rentTx = new Transaction
                    {
                        TransactionCode = $"TX-RENT-{schedule.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                        TransactionDate = dueDate,
                        TransactionType = TransactionType.MonthlyRentCharge,
                        RentAgreementId = agreement.Id,
                        PropertyUnitId = agreement.PropertyUnitId,
                        TenantId = agreement.TenantId,
                        Debit = agreement.MonthlyRent,
                        Credit = 0,
                        Description = $"Rent for {monthYear} ({propName} - {unitName})",
                        Remarks = $"Monthly rent demanded for {monthYear}",
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.Now
                    };

                    db.Transactions.Add(rentTx);
                    generated++;
                    totalDemanded += agreement.MonthlyRent;
                }

                db.SaveChanges();
                transaction.Commit();

                if (generated > 0)
                {
                    AuditService.Log("Generate Rent", "RentSchedule", monthYear, $"Generated {generated} monthly rent records for {monthYear}. Total demanded: {totalDemanded:N0}");
                }

                return (generated, existing, totalDemanded);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AuditService.Log("Rent Gen Error", "RentSchedule", monthYear, $"Error generating rent: {ex.Message}");
                throw;
            }
        }

        public static List<RentSchedule> GetRentSchedules(int year, int month, int? propertyId = null, RentScheduleStatus? status = null)
        {
            string monthYear = $"{year:D4}-{month:D2}";
            using var db = new AppDbContext();
            var query = db.RentSchedules
                          .Include(s => s.RentAgreement)
                          .ThenInclude(a => a!.Tenant)
                          .Include(s => s.RentAgreement)
                          .ThenInclude(a => a!.PropertyUnit)
                          .ThenInclude(u => u!.Property)
                          .AsNoTracking()
                          .Where(s => s.MonthYear == monthYear);

            if (propertyId.HasValue && propertyId.Value > 0)
            {
                query = query.Where(s => s.RentAgreement != null &&
                                         s.RentAgreement.PropertyUnit != null &&
                                         s.RentAgreement.PropertyUnit.PropertyId == propertyId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            return query.OrderBy(s => s.RentAgreement!.PropertyUnit!.UnitNumber).ToList();
        }

        public static (decimal Expected, decimal Received, decimal Pending, decimal Overdue, int PaidCount, int PartialCount, int PendingCount, int OverdueCount) GetMonthlyRentSummary(int year, int month)
        {
            string monthYear = $"{year:D4}-{month:D2}";
            using var db = new AppDbContext();
            var schedules = db.RentSchedules.AsNoTracking().Where(s => s.MonthYear == monthYear).ToList();

            decimal expected = schedules.Sum(s => s.TotalDue);
            decimal received = schedules.Sum(s => s.AmountPaid);
            decimal pending = schedules.Sum(s => s.Balance);

            DateTime today = DateTime.Now.Date;
            decimal overdue = schedules.Where(s => s.Balance > 0 && s.DueDate.Date < today).Sum(s => s.Balance);

            int paidCount = schedules.Count(s => s.Status == RentScheduleStatus.Paid);
            int partialCount = schedules.Count(s => s.Status == RentScheduleStatus.Partial);
            int overdueCount = schedules.Count(s => s.Balance > 0 && s.DueDate.Date < today);
            int pendingCount = schedules.Count(s => s.Balance > 0 && s.DueDate.Date >= today);

            return (expected, received, pending, overdue, paidCount, partialCount, pendingCount, overdueCount);
        }

        public static (bool Success, string Message) UpdateScheduleCharges(int scheduleId, decimal utilityCharges, decimal maintenanceCharges, decimal lateFee)
        {
            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var schedule = db.RentSchedules
                                 .Include(s => s.RentAgreement)
                                 .FirstOrDefault(s => s.Id == scheduleId);
                if (schedule == null) return (false, "Rent schedule record not found.");

                decimal oldTotal = schedule.TotalDue;
                schedule.UtilityCharges = utilityCharges;
                schedule.MaintenanceCharges = maintenanceCharges;
                schedule.LateFee = lateFee;
                schedule.TotalDue = schedule.BaseRent + utilityCharges + maintenanceCharges + lateFee;
                schedule.Balance = schedule.TotalDue - schedule.AmountPaid;

                if (schedule.Balance <= 0)
                {
                    schedule.Status = RentScheduleStatus.Paid;
                }
                else if (schedule.AmountPaid > 0)
                {
                    schedule.Status = RentScheduleStatus.Partial;
                }
                else if (schedule.DueDate.Date < DateTime.Now.Date)
                {
                    schedule.Status = RentScheduleStatus.Overdue;
                }
                else
                {
                    schedule.Status = RentScheduleStatus.Pending;
                }

                decimal diff = schedule.TotalDue - oldTotal;
                if (diff != 0)
                {
                    // Post adjustment debit for additional charges
                    int userId = AuthService.CurrentUser?.Id ?? 1;
                    var chargeTx = new Transaction
                    {
                        TransactionCode = $"TX-CHG-{schedule.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                        TransactionDate = DateTime.Now,
                        TransactionType = TransactionType.UtilityBill,
                        RentAgreementId = schedule.RentAgreementId,
                        PropertyUnitId = schedule.RentAgreement?.PropertyUnitId,
                        TenantId = schedule.RentAgreement?.TenantId,
                        Debit = diff > 0 ? diff : 0,
                        Credit = diff < 0 ? Math.Abs(diff) : 0,
                        Description = $"Additional charges updated for {schedule.MonthYear} (Utility: {utilityCharges:N0}, Maint: {maintenanceCharges:N0}, Fee: {lateFee:N0})",
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.Now
                    };
                    db.Transactions.Add(chargeTx);
                }

                db.SaveChanges();
                transaction.Commit();
                AuditService.Log("Update Rent Charges", "RentSchedule", scheduleId.ToString(), $"Updated charges for {schedule.MonthYear}. New Total: {schedule.TotalDue:N0}");
                return (true, "Charges updated successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return (false, $"Failed to update charges: {ex.Message}");
            }
        }
    }
}
