using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class LeaseService
    {
        public static string GenerateNextAgreementCode()
        {
            using var db = new AppDbContext();
            int count = db.RentAgreements.Count() + 1;
            string code = $"AGR-{count:D3}";
            while (db.RentAgreements.Any(a => a.AgreementCode == code))
            {
                count++;
                code = $"AGR-{count:D3}";
            }
            return code;
        }

        public static List<RentAgreement> GetAllAgreements(AgreementStatus? status = null, int? propertyId = null)
        {
            using var db = new AppDbContext();
            var query = db.RentAgreements
                          .Include(a => a.Tenant)
                          .Include(a => a.PropertyUnit)
                          .ThenInclude(u => u!.Property)
                          .Include(a => a.RentRateHistories)
                          .AsNoTracking()
                          .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }

            if (propertyId.HasValue)
            {
                query = query.Where(a => a.PropertyUnit != null && a.PropertyUnit.PropertyId == propertyId.Value);
            }

            return query.OrderByDescending(a => a.StartDate).ToList();
        }

        public static RentAgreement? GetAgreementById(int agreementId)
        {
            using var db = new AppDbContext();
            return db.RentAgreements
                     .Include(a => a.Tenant)
                     .Include(a => a.PropertyUnit)
                     .ThenInclude(u => u!.Property)
                     .Include(a => a.RentRateHistories.OrderByDescending(r => r.EffectiveDate))
                     .Include(a => a.RentSchedules.OrderByDescending(s => s.MonthYear))
                     .FirstOrDefault(a => a.Id == agreementId);
        }

        public static (bool Success, string Message, RentAgreement? Agreement) CreateAgreement(RentAgreement model, bool postInitialDepositTransactions = true)
        {
            if (model.TenantId <= 0) return (false, "Please select a tenant.", null);
            if (model.PropertyUnitId <= 0) return (false, "Please select a property unit.", null);
            if (model.MonthlyRent <= 0) return (false, "Monthly rent must be greater than zero.", null);
            if (model.DueDayOfMonth < 1 || model.DueDayOfMonth > 31) model.DueDayOfMonth = 5;

            using var db = new AppDbContext();
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var unit = db.PropertyUnits.Include(u => u.Property).FirstOrDefault(u => u.Id == model.PropertyUnitId);
                if (unit == null) return (false, "Property unit not found.", null);

                // Check if unit is already occupied by an active agreement
                bool alreadyOccupied = db.RentAgreements.Any(a => a.PropertyUnitId == model.PropertyUnitId &&
                                                                  a.Status == AgreementStatus.Active &&
                                                                  a.Id != model.Id);
                if (alreadyOccupied)
                {
                    return (false, $"Unit '{unit.UnitNumber}' is already occupied by an active agreement.", null);
                }

                if (string.IsNullOrWhiteSpace(model.AgreementCode))
                {
                    model.AgreementCode = GenerateNextAgreementCode();
                }

                model.Status = AgreementStatus.Active;
                model.CreatedAt = DateTime.Now;
                db.RentAgreements.Add(model);
                db.SaveChanges();

                // Mark unit as occupied
                unit.Status = UnitStatus.Occupied;
                db.SaveChanges();

                // Record initial rent rate in history
                var initialRate = new RentRateHistory
                {
                    RentAgreementId = model.Id,
                    OldRent = 0,
                    NewRent = model.MonthlyRent,
                    EffectiveDate = model.StartDate,
                    Reason = "Initial Agreement Rent",
                    CreatedAt = DateTime.Now
                };
                db.RentRateHistories.Add(initialRate);
                db.SaveChanges();

                // Post initial transactions if requested
                if (postInitialDepositTransactions)
                {
                    int userId = AuthService.CurrentUser?.Id ?? 1;

                    if (model.SecurityDeposit > 0)
                    {
                        var secTx = new Transaction
                        {
                            TransactionCode = $"TX-SEC-{model.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                            TransactionDate = model.StartDate,
                            TransactionType = TransactionType.SecurityDeposit,
                            RentAgreementId = model.Id,
                            PropertyUnitId = model.PropertyUnitId,
                            TenantId = model.TenantId,
                            Debit = 0,
                            Credit = model.SecurityDeposit,
                            PaymentMethod = PaymentMethod.Cash,
                            Description = $"Security Deposit Received for {unit.UnitNumber}",
                            Remarks = "Initial security deposit",
                            CreatedByUserId = userId,
                            CreatedAt = DateTime.Now
                        };
                        db.Transactions.Add(secTx);
                    }

                    if (model.AdvanceAmount > 0)
                    {
                        var advTx = new Transaction
                        {
                            TransactionCode = $"TX-ADV-{model.Id}-{DateTime.Now:yyyyMMddHHmmss}",
                            TransactionDate = model.StartDate,
                            TransactionType = TransactionType.AdvanceRent,
                            RentAgreementId = model.Id,
                            PropertyUnitId = model.PropertyUnitId,
                            TenantId = model.TenantId,
                            Debit = 0,
                            Credit = model.AdvanceAmount,
                            PaymentMethod = PaymentMethod.Cash,
                            Description = $"Advance Rent Received for {unit.UnitNumber}",
                            Remarks = "Initial advance rent payment",
                            CreatedByUserId = userId,
                            CreatedAt = DateTime.Now
                        };
                        db.Transactions.Add(advTx);
                    }

                    db.SaveChanges();
                }

                transaction.Commit();
                AuditService.Log("Create Agreement", "RentAgreement", model.Id.ToString(), $"Created lease agreement '{model.AgreementCode}' for Unit ID {model.PropertyUnitId}");
                return (true, "Rent Agreement created successfully.", model);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return (false, $"Failed to create agreement: {ex.Message}", null);
            }
        }

        public static (bool Success, string Message) UpdateRentRate(int agreementId, decimal newRent, DateTime effectiveDate, string reason)
        {
            if (newRent <= 0) return (false, "New rent must be greater than zero.");

            using var db = new AppDbContext();
            var agreement = db.RentAgreements.Find(agreementId);
            if (agreement == null) return (false, "Agreement not found.");

            if (agreement.MonthlyRent == newRent)
            {
                return (false, "New rent is the same as current monthly rent.");
            }

            decimal oldRent = agreement.MonthlyRent;
            agreement.MonthlyRent = newRent;

            var history = new RentRateHistory
            {
                RentAgreementId = agreementId,
                OldRent = oldRent,
                NewRent = newRent,
                EffectiveDate = effectiveDate,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Periodic Rent Increase" : reason.Trim(),
                CreatedAt = DateTime.Now
            };

            db.RentRateHistories.Add(history);
            db.SaveChanges();

            AuditService.Log("Rent Rate Change", "RentAgreement", agreementId.ToString(), $"Updated rent from {oldRent:N0} to {newRent:N0} effective {effectiveDate:dd/MM/yyyy}. Reason: {reason}");
            return (true, $"Monthly rent successfully updated from {oldRent:N0} to {newRent:N0}.");
        }

        public static (bool Success, string Message) TerminateAgreement(int agreementId, string remarks)
        {
            using var db = new AppDbContext();
            var agreement = db.RentAgreements.Include(a => a.PropertyUnit).FirstOrDefault(a => a.Id == agreementId);
            if (agreement == null) return (false, "Agreement not found.");

            agreement.Status = AgreementStatus.Terminated;
            agreement.Remarks = string.IsNullOrWhiteSpace(agreement.Remarks)
                ? $"Terminated on {DateTime.Now:dd/MM/yyyy}: {remarks}"
                : $"{agreement.Remarks} | Terminated on {DateTime.Now:dd/MM/yyyy}: {remarks}";

            if (agreement.PropertyUnit != null)
            {
                // Check if any other active agreement exists for unit
                bool otherActive = db.RentAgreements.Any(a => a.PropertyUnitId == agreement.PropertyUnitId &&
                                                              a.Status == AgreementStatus.Active &&
                                                              a.Id != agreementId);
                if (!otherActive)
                {
                    agreement.PropertyUnit.Status = UnitStatus.Vacant;
                }
            }

            db.SaveChanges();
            AuditService.Log("Terminate Agreement", "RentAgreement", agreementId.ToString(), $"Terminated lease agreement '{agreement.AgreementCode}'. Reason: {remarks}");
            return (true, "Agreement terminated and unit marked as Vacant.");
        }
    }
}
