using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class CustomerCreditProfile
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? CnicOrId { get; set; }
        public string? City { get; set; }
        public string? PermanentAddress { get; set; }
        public CustomerType CustomerType { get; set; }
        public string Rating { get; set; } = "Good";
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal CurrentOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public int ActivePlansCount { get; set; }
        public int ClosedPlansCount { get; set; }
        public int MissedInstallmentsCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? NextDueDate { get; set; }

        public string? GuarantorName { get; set; }
        public string? GuarantorPhone { get; set; }
        public string? GuarantorCnic { get; set; }
        public string? GuarantorRelation { get; set; }

        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
    }

    public class DefaulterItem
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? CnicOrId { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public DateTime OldestOverdueDate { get; set; }
        public int DaysOverdue { get; set; }
        public int MissedInstallmentsCount { get; set; }
        public string ReferenceDetails { get; set; } = string.Empty;
        public string Rating { get; set; } = "Fair";
        public string Bucket { get; set; } = "1-7 Days"; // "1-7 Days", "8-30 Days", "31-60 Days", "60+ Days"
        public DateTime? LastPaymentDate { get; set; }
    }

    public class ReceivablesService
    {
        public CustomerCreditProfile? GetCustomerCreditProfile(int customerId)
        {
            using var db = new AppDbContext();
            var customer = db.Tenants
                .Include(t => t.RentAgreements).ThenInclude(a => a.RentSchedules)
                .Include(t => t.InstallmentSales).ThenInclude(s => s.Schedules)
                .Include(t => t.Transactions)
                .FirstOrDefault(t => t.Id == customerId);

            if (customer == null) return null;

            var validTx = customer.Transactions.Where(t => !t.IsVoided).ToList();
            decimal totalDebit = validTx.Sum(t => t.Debit);
            decimal totalCredit = validTx.Sum(t => t.Credit);
            decimal outstanding = totalDebit - totalCredit;

            DateTime today = DateTime.Today;

            // Overdue rent schedules
            decimal overdueRent = customer.RentAgreements
                .Where(a => a.Status == AgreementStatus.Active)
                .SelectMany(a => a.RentSchedules)
                .Where(s => s.DueDate < today && s.Balance > 0)
                .Sum(s => s.Balance);

            int missedRentCount = customer.RentAgreements
                .Where(a => a.Status == AgreementStatus.Active)
                .SelectMany(a => a.RentSchedules)
                .Count(s => s.DueDate < today && s.Balance > 0);

            // Overdue installment schedules
            decimal overdueInstallment = customer.InstallmentSales
                .Where(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid || s.Status == InstallmentPlanStatus.Overdue)
                .SelectMany(s => s.Schedules)
                .Where(sch => sch.DueDate < today && sch.RemainingAmount > 0)
                .Sum(sch => sch.RemainingAmount);

            int missedInstallmentCount = customer.InstallmentSales
                .Where(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid || s.Status == InstallmentPlanStatus.Overdue)
                .SelectMany(s => s.Schedules)
                .Count(sch => sch.DueDate < today && sch.RemainingAmount > 0);

            decimal totalOverdue = overdueRent + overdueInstallment;
            int totalMissed = missedRentCount + missedInstallmentCount;

            var lastPayment = validTx
                .Where(t => t.Credit > 0)
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();

            // Next due date
            DateTime? nextRentDue = customer.RentAgreements
                .Where(a => a.Status == AgreementStatus.Active)
                .SelectMany(a => a.RentSchedules)
                .Where(s => s.DueDate >= today && s.Balance > 0)
                .OrderBy(s => s.DueDate)
                .Select(s => (DateTime?)s.DueDate)
                .FirstOrDefault();

            DateTime? nextInstDue = customer.InstallmentSales
                .Where(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid)
                .SelectMany(s => s.Schedules)
                .Where(s => s.DueDate >= today && s.RemainingAmount > 0)
                .OrderBy(s => s.DueDate)
                .Select(s => (DateTime?)s.DueDate)
                .FirstOrDefault();

            DateTime? nextDue = null;
            if (nextRentDue.HasValue && nextInstDue.HasValue)
                nextDue = nextRentDue.Value < nextInstDue.Value ? nextRentDue.Value : nextInstDue.Value;
            else
                nextDue = nextRentDue ?? nextInstDue;

            int activePlans = customer.RentAgreements.Count(a => a.Status == AgreementStatus.Active) +
                              customer.InstallmentSales.Count(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid);

            int closedPlans = customer.RentAgreements.Count(a => a.Status == AgreementStatus.Expired || a.Status == AgreementStatus.Terminated) +
                              customer.InstallmentSales.Count(s => s.Status == InstallmentPlanStatus.Completed || s.Status == InstallmentPlanStatus.Settled);

            decimal availCredit = customer.CreditLimit > 0
                ? Math.Max(0, customer.CreditLimit - outstanding)
                : 0;

            return new CustomerCreditProfile
            {
                CustomerId = customer.Id,
                CustomerCode = customer.TenantCode,
                FullName = customer.FullName,
                ContactNumber = customer.ContactNumber,
                CnicOrId = customer.CnicOrId,
                City = customer.City,
                PermanentAddress = customer.PermanentAddress,
                CustomerType = customer.CustomerType,
                Rating = string.IsNullOrWhiteSpace(customer.Rating) ? (totalMissed > 2 ? "Risky" : "Good") : customer.Rating,
                CreditLimit = customer.CreditLimit,
                AvailableCredit = availCredit,
                TotalPurchases = totalDebit,
                TotalPaid = totalCredit,
                CurrentOutstanding = outstanding,
                OverdueAmount = totalOverdue,
                ActivePlansCount = activePlans,
                ClosedPlansCount = closedPlans,
                MissedInstallmentsCount = totalMissed,
                LastPaymentDate = lastPayment?.TransactionDate,
                NextDueDate = nextDue,
                GuarantorName = customer.GuarantorName,
                GuarantorPhone = customer.GuarantorPhone,
                GuarantorCnic = customer.GuarantorCnic,
                GuarantorRelation = customer.GuarantorRelation,
                RecentTransactions = validTx.OrderByDescending(t => t.TransactionDate).Take(20).ToList()
            };
        }

        public List<DefaulterItem> GetDefaultersList(int minDays = 1, int? maxDays = null, string? search = null, string? bucketFilter = null)
        {
            using var db = new AppDbContext();
            DateTime today = DateTime.Today;

            var customers = db.Tenants
                .Include(t => t.RentAgreements).ThenInclude(a => a.PropertyUnit).ThenInclude(u => u!.Property)
                .Include(t => t.RentAgreements).ThenInclude(a => a.RentSchedules)
                .Include(t => t.InstallmentSales).ThenInclude(s => s.Items).ThenInclude(i => i.Product)
                .Include(t => t.InstallmentSales).ThenInclude(s => s.Schedules)
                .Include(t => t.Transactions)
                .AsNoTracking()
                .Where(t => t.Status == TenantStatus.Active)
                .ToList();

            var defaulters = new List<DefaulterItem>();

            foreach (var cust in customers)
            {
                var validTx = cust.Transactions.Where(t => !t.IsVoided).ToList();
                decimal outstanding = validTx.Sum(t => t.Debit) - validTx.Sum(t => t.Credit);

                // Overdue rent
                var overdueRentSchedules = cust.RentAgreements
                    .Where(a => a.Status == AgreementStatus.Active)
                    .SelectMany(a => a.RentSchedules)
                    .Where(s => s.DueDate < today && s.Balance > 0)
                    .ToList();

                // Overdue installments
                var overdueInstSchedules = cust.InstallmentSales
                    .Where(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid || s.Status == InstallmentPlanStatus.Overdue)
                    .SelectMany(s => s.Schedules)
                    .Where(s => s.DueDate < today && s.RemainingAmount > 0)
                    .ToList();

                decimal totalOverdue = overdueRentSchedules.Sum(s => s.Balance) + overdueInstSchedules.Sum(s => s.RemainingAmount);
                int missedCount = overdueRentSchedules.Count + overdueInstSchedules.Count;

                if (totalOverdue <= 0 && outstanding <= 0) continue;

                // Find oldest overdue date
                DateTime oldestDue = today;
                if (overdueRentSchedules.Any())
                {
                    var oldestRent = overdueRentSchedules.Min(s => s.DueDate);
                    if (oldestRent < oldestDue) oldestDue = oldestRent;
                }
                if (overdueInstSchedules.Any())
                {
                    var oldestInst = overdueInstSchedules.Min(s => s.DueDate);
                    if (oldestInst < oldestDue) oldestDue = oldestInst;
                }

                int daysOverdue = (today - oldestDue).Days;
                if (daysOverdue < minDays) continue;
                if (maxDays.HasValue && daysOverdue > maxDays.Value) continue;

                string bucket = daysOverdue switch
                {
                    <= 7 => "1-7 Days",
                    <= 30 => "8-30 Days",
                    <= 60 => "31-60 Days",
                    _ => "60+ Days"
                };

                if (!string.IsNullOrWhiteSpace(bucketFilter) && bucketFilter != "All" && bucket != bucketFilter)
                {
                    continue;
                }

                // Reference details
                var refList = new List<string>();
                foreach (var ag in cust.RentAgreements.Where(a => a.Status == AgreementStatus.Active))
                {
                    if (ag.PropertyUnit?.Property != null)
                        refList.Add($"{ag.PropertyUnit.Property.Name} ({ag.PropertyUnit.UnitNumber})");
                }
                foreach (var s in cust.InstallmentSales.Where(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid))
                {
                    refList.AddRange(s.Items.Select(i => i.ItemDescription));
                }

                var lastPay = validTx.Where(t => t.Credit > 0).OrderByDescending(t => t.TransactionDate).FirstOrDefault();

                defaulters.Add(new DefaulterItem
                {
                    CustomerId = cust.Id,
                    CustomerCode = cust.TenantCode,
                    FullName = cust.FullName,
                    ContactNumber = cust.ContactNumber,
                    CnicOrId = cust.CnicOrId,
                    TotalOutstanding = outstanding,
                    OverdueAmount = totalOverdue > 0 ? totalOverdue : outstanding,
                    OldestOverdueDate = oldestDue,
                    DaysOverdue = daysOverdue,
                    MissedInstallmentsCount = missedCount,
                    ReferenceDetails = string.Join(", ", refList.Distinct()),
                    Rating = cust.Rating ?? (daysOverdue > 60 ? "Defaulter" : (daysOverdue > 30 ? "Risky" : "Fair")),
                    Bucket = bucket,
                    LastPaymentDate = lastPay?.TransactionDate
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                defaulters = defaulters.Where(d =>
                    d.FullName.ToLower().Contains(s) ||
                    d.ContactNumber.Contains(s) ||
                    (d.CnicOrId != null && d.CnicOrId.Contains(s)) ||
                    d.CustomerCode.ToLower().Contains(s) ||
                    d.ReferenceDetails.ToLower().Contains(s)
                ).ToList();
            }

            return defaulters.OrderByDescending(d => d.DaysOverdue).ToList();
        }

        public (int TotalCustomers, decimal TotalOutstanding, decimal DueToday, decimal TotalOverdue, decimal MonthCollection) GetReceivablesDashboardMetrics()
        {
            using var db = new AppDbContext();
            DateTime today = DateTime.Today;
            DateTime startOfMonth = new DateTime(today.Year, today.Month, 1);
            DateTime endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            int totalCustomers = db.Tenants.Count(t => t.Status == TenantStatus.Active);

            var validTx = db.Transactions.AsNoTracking().Where(t => !t.IsVoided).ToList();
            decimal totalDebit = validTx.Sum(t => t.Debit);
            decimal totalCredit = validTx.Sum(t => t.Credit);
            decimal totalOutstanding = totalDebit - totalCredit;

            decimal monthCollection = validTx
                .Where(t => t.TransactionDate >= startOfMonth && t.TransactionDate <= endOfMonth && t.Credit > 0)
                .Sum(t => t.Credit);

            // Due Today
            decimal rentDueToday = db.RentSchedules
                .Where(s => s.DueDate.Date == today && s.Balance > 0)
                .Select(s => s.Balance)
                .ToList()
                .Sum();

            decimal instDueToday = db.InstallmentSchedules
                .Where(s => s.DueDate.Date == today && s.RemainingAmount > 0)
                .Select(s => s.RemainingAmount)
                .ToList()
                .Sum();

            decimal dueToday = rentDueToday + instDueToday;

            // Total Overdue
            decimal rentOverdue = db.RentSchedules
                .Where(s => s.DueDate.Date < today && s.Balance > 0)
                .Select(s => s.Balance)
                .ToList()
                .Sum();

            decimal instOverdue = db.InstallmentSchedules
                .Where(s => s.DueDate.Date < today && s.RemainingAmount > 0)
                .Select(s => s.RemainingAmount)
                .ToList()
                .Sum();

            decimal totalOverdue = rentOverdue + instOverdue;

            return (totalCustomers, totalOutstanding, dueToday, totalOverdue, monthCollection);
        }
    }
}
