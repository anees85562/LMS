using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class LedgerEntryDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string? Remarks { get; set; }
        public bool IsVoided { get; set; }
    }

    public class TenantLedgerStatement
    {
        public Tenant? Tenant { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<LedgerEntryDto> Entries { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }

    public class TraditionalRegisterRow
    {
        public int AgreementId { get; set; }
        public int TenantId { get; set; }
        public string TenantCode { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
        public decimal OtherCharges { get; set; }
        public decimal CurrentRentDemanded { get; set; }
        public decimal PreviousArrears { get; set; }
        public decimal TotalDemanded { get; set; }
        public decimal PaidThisMonth { get; set; }
        public decimal NetBalance { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? LastPaymentDate { get; set; }
        public string? LastReceiptNumber { get; set; }
        public string? Remarks { get; set; }
    }

    public class LedgerService
    {
        public static TenantLedgerStatement GetTenantLedger(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var db = new AppDbContext();
            var tenant = db.Tenants
                           .Include(t => t.RentAgreements)
                           .ThenInclude(a => a.PropertyUnit)
                           .ThenInclude(u => u!.Property)
                           .FirstOrDefault(t => t.Id == tenantId);

            var statement = new TenantLedgerStatement
            {
                Tenant = tenant,
                FromDate = fromDate,
                ToDate = toDate
            };

            var allTxs = db.Transactions
                           .Where(t => t.TenantId == tenantId && !t.IsVoided)
                           .OrderBy(t => t.TransactionDate)
                           .ThenBy(t => t.Id)
                           .ToList();

            decimal runningBal = 0;
            decimal openingBal = 0;

            foreach (var tx in allTxs)
            {
                if (fromDate.HasValue && tx.TransactionDate.Date < fromDate.Value.Date)
                {
                    openingBal += (tx.Debit - tx.Credit);
                    runningBal = openingBal;
                    continue;
                }

                if (toDate.HasValue && tx.TransactionDate.Date > toDate.Value.Date)
                {
                    continue;
                }

                runningBal += (tx.Debit - tx.Credit);

                statement.Entries.Add(new LedgerEntryDto
                {
                    TransactionId = tx.Id,
                    TransactionCode = tx.TransactionCode,
                    Date = tx.TransactionDate,
                    TypeName = tx.TransactionType.ToString(),
                    Description = tx.Description,
                    Reference = tx.ReferenceNumber,
                    PaymentMethod = tx.PaymentMethod?.ToString() ?? "-",
                    Debit = tx.Debit,
                    Credit = tx.Credit,
                    Balance = runningBal,
                    Remarks = tx.Remarks,
                    IsVoided = tx.IsVoided
                });
            }

            statement.OpeningBalance = openingBal;
            statement.TotalDebit = statement.Entries.Sum(e => e.Debit);
            statement.TotalCredit = statement.Entries.Sum(e => e.Credit);
            statement.ClosingBalance = statement.OpeningBalance + statement.TotalDebit - statement.TotalCredit;

            return statement;
        }

        public static List<TraditionalRegisterRow> GetTraditionalRegisterMatrix(int year, int month, int? propertyId = null)
        {
            string monthYear = $"{year:D4}-{month:D2}";
            DateTime monthStart = new DateTime(year, month, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            using var db = new AppDbContext();
            var agreementsQuery = db.RentAgreements
                                    .Include(a => a.Tenant)
                                    .Include(a => a.PropertyUnit)
                                    .ThenInclude(u => u!.Property)
                                    .Include(a => a.RentSchedules)
                                    .Where(a => a.Status == AgreementStatus.Active &&
                                                a.StartDate <= monthEnd &&
                                                (!a.EndDate.HasValue || a.EndDate.Value >= monthStart))
                                    .AsQueryable();

            if (propertyId.HasValue && propertyId.Value > 0)
            {
                agreementsQuery = agreementsQuery.Where(a => a.PropertyUnit != null && a.PropertyUnit.PropertyId == propertyId.Value);
            }

            var agreements = agreementsQuery.ToList();
            var rows = new List<TraditionalRegisterRow>();

            foreach (var a in agreements)
            {
                var schedule = a.RentSchedules.FirstOrDefault(s => s.MonthYear == monthYear);

                decimal monthlyRent = schedule?.BaseRent ?? a.MonthlyRent;
                decimal extraCharges = (schedule?.UtilityCharges ?? 0) + (schedule?.MaintenanceCharges ?? 0) + (schedule?.LateFee ?? 0);
                decimal currentDemanded = schedule?.TotalDue ?? monthlyRent;

                // Compute arrears before this month: sum of past schedules unpaid balances or past transaction debits - credits
                var pastTxs = db.Transactions
                                .Where(t => t.TenantId == a.TenantId &&
                                            !t.IsVoided &&
                                            t.TransactionDate < monthStart)
                                .ToList();
                decimal pastDebit = pastTxs.Sum(t => t.Debit);
                decimal pastCredit = pastTxs.Sum(t => t.Credit);
                decimal previousArrears = Math.Max(0, pastDebit - pastCredit);

                decimal totalDemanded = currentDemanded + previousArrears;

                // Month payments
                var monthPayments = db.Transactions
                                      .Where(t => t.TenantId == a.TenantId &&
                                                  !t.IsVoided &&
                                                  t.TransactionType == TransactionType.RentPayment &&
                                                  t.TransactionDate >= monthStart &&
                                                  t.TransactionDate <= monthEnd)
                                      .OrderByDescending(t => t.TransactionDate)
                                      .ToList();

                decimal paidThisMonth = monthPayments.Sum(t => t.Credit);
                decimal netBalance = totalDemanded - paidThisMonth;

                // Overall balance across all time
                decimal overallBal = TenantService.GetTenantCurrentBalance(a.TenantId);

                string status;
                if (overallBal <= 0 && paidThisMonth >= currentDemanded)
                {
                    status = overallBal < 0 ? "Advance" : "Paid";
                }
                else if (paidThisMonth > 0)
                {
                    status = "Partial";
                }
                else if (DateTime.Now.Date > (schedule?.DueDate.Date ?? new DateTime(year, month, Math.Min(a.DueDayOfMonth, DateTime.DaysInMonth(year, month)))))
                {
                    status = "Overdue";
                }
                else
                {
                    status = "Pending";
                }

                var lastPay = monthPayments.FirstOrDefault();
                var lastReceipt = lastPay != null
                    ? db.PaymentReceipts.FirstOrDefault(r => r.TransactionId == lastPay.Id)?.ReceiptNumber
                    : null;

                rows.Add(new TraditionalRegisterRow
                {
                    AgreementId = a.Id,
                    TenantId = a.TenantId,
                    TenantCode = a.Tenant?.TenantCode ?? "",
                    TenantName = a.Tenant?.FullName ?? "",
                    Phone = a.Tenant?.ContactNumber ?? "",
                    PropertyName = a.PropertyUnit?.Property?.Name ?? "",
                    UnitNumber = a.PropertyUnit?.UnitNumber ?? "",
                    MonthlyRent = monthlyRent,
                    OtherCharges = extraCharges,
                    CurrentRentDemanded = currentDemanded,
                    PreviousArrears = previousArrears,
                    TotalDemanded = totalDemanded,
                    PaidThisMonth = paidThisMonth,
                    NetBalance = netBalance,
                    Status = status,
                    LastPaymentDate = lastPay?.TransactionDate,
                    LastReceiptNumber = lastReceipt,
                    Remarks = a.Remarks
                });
            }

            return rows.OrderBy(r => r.PropertyName).ThenBy(r => r.UnitNumber).ToList();
        }
    }
}
