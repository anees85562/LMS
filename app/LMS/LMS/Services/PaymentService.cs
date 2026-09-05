using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class PaymentDto
    {
        public int TenantId { get; set; }
        public int? RentAgreementId { get; set; }
        public int? PropertyUnitId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
        public string? ReferenceNumber { get; set; }
        public string? BankName { get; set; }
        public string? RentalPeriod { get; set; }
        public string? Remarks { get; set; }
    }

    public class PaymentService
    {
        public static string GenerateNextReceiptNumber()
        {
            using var db = new AppDbContext();
            string prefix = SettingService.Get("Receipt.Prefix", "RCP");
            string year = DateTime.Now.ToString("yyyy");
            string baseCode = $"{prefix}-{year}-";

            int count = db.PaymentReceipts.Count(r => r.ReceiptNumber.StartsWith(baseCode)) + 1;
            string receiptNum = $"{baseCode}{count:D5}";
            while (db.PaymentReceipts.Any(r => r.ReceiptNumber == receiptNum))
            {
                count++;
                receiptNum = $"{baseCode}{count:D5}";
            }
            return receiptNum;
        }

        public static (bool Success, string Message, PaymentReceipt? Receipt) RecordPayment(PaymentDto dto)
        {
            if (dto.TenantId <= 0) return (false, "Please select a valid tenant.", null);
            if (dto.Amount <= 0) return (false, "Payment amount must be greater than zero.", null);

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var tenant = db.Tenants
                               .Include(t => t.RentAgreements)
                               .ThenInclude(a => a.PropertyUnit)
                               .ThenInclude(u => u!.Property)
                               .FirstOrDefault(t => t.Id == dto.TenantId);

                if (tenant == null) return (false, "Tenant not found.", null);

                // Auto resolve agreement and unit if not specified
                var agreement = dto.RentAgreementId.HasValue
                    ? tenant.RentAgreements.FirstOrDefault(a => a.Id == dto.RentAgreementId.Value)
                    : tenant.RentAgreements.FirstOrDefault(a => a.Status == AgreementStatus.Active);

                int? unitId = dto.PropertyUnitId ?? agreement?.PropertyUnitId;

                // 1. Calculate previous tenant balance
                var existingNonVoidTxs = db.Transactions
                                           .Where(t => t.TenantId == dto.TenantId && !t.IsVoided)
                                           .ToList();
                decimal prevDebit = existingNonVoidTxs.Sum(t => t.Debit);
                decimal prevCredit = existingNonVoidTxs.Sum(t => t.Credit);
                decimal previousBalance = prevDebit - prevCredit;
                decimal remainingBalance = previousBalance - dto.Amount;

                int userId = AuthService.CurrentUser?.Id ?? 1;
                string userName = AuthService.CurrentUser?.FullName ?? "Staff";

                // 2. Insert Credit Transaction
                string txCode = $"TX-PAY-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
                string unitLabel = agreement?.PropertyUnit?.UnitNumber ?? "Unit";
                string propLabel = agreement?.PropertyUnit?.Property?.Name ?? "";

                string desc = $"Rent Payment Received ({propLabel} - {unitLabel})";
                if (!string.IsNullOrWhiteSpace(dto.RentalPeriod))
                {
                    desc += $" for {dto.RentalPeriod}";
                }

                var payTx = new Transaction
                {
                    TransactionCode = txCode,
                    TransactionDate = dto.PaymentDate,
                    TransactionType = TransactionType.RentPayment,
                    RentAgreementId = agreement?.Id,
                    PropertyUnitId = unitId,
                    TenantId = dto.TenantId,
                    Debit = 0,
                    Credit = dto.Amount,
                    RunningBalance = remainingBalance,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.ReferenceNumber?.Trim(),
                    BankName = dto.BankName?.Trim(),
                    Description = desc,
                    Remarks = dto.Remarks?.Trim(),
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.Now
                };

                db.Transactions.Add(payTx);
                db.SaveChanges(); // Persist to get payTx.Id

                // 3. Allocate payment to oldest unpaid / partial RentSchedules for this agreement
                if (agreement != null)
                {
                    var unpaidSchedules = db.RentSchedules
                                            .Where(s => s.RentAgreementId == agreement.Id && s.Balance > 0)
                                            .OrderBy(s => s.MonthYear)
                                            .ToList();

                    decimal unallocated = dto.Amount;
                    foreach (var schedule in unpaidSchedules)
                    {
                        if (unallocated <= 0) break;

                        decimal amountToApply = Math.Min(schedule.Balance, unallocated);
                        schedule.AmountPaid += amountToApply;
                        schedule.Balance = schedule.TotalDue - schedule.AmountPaid;
                        unallocated -= amountToApply;

                        if (schedule.Balance <= 0)
                        {
                            schedule.Status = RentScheduleStatus.Paid;
                        }
                        else
                        {
                            schedule.Status = RentScheduleStatus.Partial;
                        }
                    }
                }

                // 4. Create PaymentReceipt
                string receiptNum = GenerateNextReceiptNumber();
                var receipt = new PaymentReceipt
                {
                    ReceiptNumber = receiptNum,
                    TransactionId = payTx.Id,
                    TenantId = dto.TenantId,
                    PropertyUnitId = unitId ?? 0,
                    PaymentDate = dto.PaymentDate,
                    AmountPaid = dto.Amount,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNumber = dto.ReferenceNumber?.Trim(),
                    BankName = dto.BankName?.Trim(),
                    RentalPeriod = dto.RentalPeriod ?? DateTime.Now.ToString("MMMM yyyy"),
                    PreviousBalance = previousBalance,
                    CurrentPayment = dto.Amount,
                    RemainingBalance = remainingBalance,
                    ReceivedByUserId = userId,
                    ReceivedByUserName = userName,
                    Remarks = dto.Remarks?.Trim(),
                    CreatedAt = DateTime.Now
                };

                db.PaymentReceipts.Add(receipt);
                db.SaveChanges();

                transaction.Commit();

                AuditService.Log("Record Payment", "Transaction", payTx.Id.ToString(), $"Recorded payment of {dto.Amount:N2} via {dto.PaymentMethod} for tenant '{tenant.FullName}'. Receipt: {receiptNum}");

                return (true, "Payment recorded successfully.", receipt);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                AuditService.Log("Payment Error", "Transaction", null, $"Error recording payment: {ex.Message}");
                return (false, $"Failed to record payment: {ex.Message}", null);
            }
        }

        public static (bool Success, string Message) VoidTransaction(int transactionId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return (false, "A reason must be provided to void/reverse a transaction.");
            }

            using var db = new AppDbContext();
            using var dbTx = db.Database.BeginTransaction();
            try
            {
                var tx = db.Transactions
                           .Include(t => t.RentAgreement)
                           .Include(t => t.Tenant)
                           .FirstOrDefault(t => t.Id == transactionId);

                if (tx == null) return (false, "Transaction not found.");
                if (tx.IsVoided) return (false, "This transaction has already been voided.");

                int userId = AuthService.CurrentUser?.Id ?? 1;

                // Mark original transaction as voided
                tx.IsVoided = true;
                tx.VoidReason = reason.Trim();
                tx.VoidDate = DateTime.Now;
                tx.VoidedByUserId = userId;

                // If it was a RentPayment, reverse schedule allocation
                if (tx.TransactionType == TransactionType.RentPayment && tx.Credit > 0 && tx.RentAgreementId.HasValue)
                {
                    // Reverse from newest paid schedules backwards
                    var schedules = db.RentSchedules
                                      .Where(s => s.RentAgreementId == tx.RentAgreementId.Value && s.AmountPaid > 0)
                                      .OrderByDescending(s => s.MonthYear)
                                      .ToList();

                    decimal amountToReverse = tx.Credit;
                    foreach (var s in schedules)
                    {
                        if (amountToReverse <= 0) break;

                        decimal reduce = Math.Min(s.AmountPaid, amountToReverse);
                        s.AmountPaid -= reduce;
                        s.Balance = s.TotalDue - s.AmountPaid;
                        amountToReverse -= reduce;

                        if (s.AmountPaid == 0)
                        {
                            s.Status = (DateTime.Now.Date > s.DueDate.Date) ? RentScheduleStatus.Overdue : RentScheduleStatus.Pending;
                        }
                        else if (s.Balance > 0)
                        {
                            s.Status = RentScheduleStatus.Partial;
                        }
                        else
                        {
                            s.Status = RentScheduleStatus.Paid;
                        }
                    }
                }

                // If it was a MonthlyRentCharge, adjust the schedule
                if (tx.TransactionType == TransactionType.MonthlyRentCharge && tx.Debit > 0 && tx.RentAgreementId.HasValue)
                {
                    // Rent charge reversal
                }

                // Post a reversal Adjustment entry to ensure running balance audit trail is documented
                var reversalTx = new Transaction
                {
                    TransactionCode = $"TX-REV-{tx.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.Adjustment,
                    RentAgreementId = tx.RentAgreementId,
                    PropertyUnitId = tx.PropertyUnitId,
                    TenantId = tx.TenantId,
                    Debit = 0,
                    Credit = 0,
                    Description = $"Reversal of {tx.TransactionCode} ({tx.Description})",
                    Remarks = $"Voided Reason: {reason.Trim()}",
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.Now
                };

                db.Transactions.Add(reversalTx);
                db.SaveChanges();
                dbTx.Commit();

                AuditService.Log("Void Transaction", "Transaction", transactionId.ToString(), $"Voided transaction '{tx.TransactionCode}'. Reason: {reason.Trim()}");
                return (true, "Transaction voided and reversal ledger entry posted successfully.");
            }
            catch (Exception ex)
            {
                dbTx.Rollback();
                return (false, $"Failed to void transaction: {ex.Message}");
            }
        }

        public static PaymentReceipt? GetReceiptById(int receiptId)
        {
            using var db = new AppDbContext();
            return db.PaymentReceipts
                     .Include(r => r.Tenant)
                     .Include(r => r.PropertyUnit)
                     .ThenInclude(u => u!.Property)
                     .Include(r => r.Transaction)
                     .FirstOrDefault(r => r.Id == receiptId);
        }

        public static List<PaymentReceipt> GetAllReceipts(int? tenantId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var db = new AppDbContext();
            var query = db.PaymentReceipts
                          .Include(r => r.Tenant)
                          .Include(r => r.PropertyUnit)
                          .ThenInclude(u => u!.Property)
                          .Include(r => r.Transaction)
                          .AsNoTracking()
                          .AsQueryable();

            if (tenantId.HasValue && tenantId.Value > 0)
            {
                query = query.Where(r => r.TenantId == tenantId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.PaymentDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.PaymentDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            return query.OrderByDescending(r => r.PaymentDate).ToList();
        }
    }
}
