using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class TenantService
    {
        public static string GenerateNextTenantCode(CustomerType customerType = CustomerType.Tenant)
        {
            using var db = new AppDbContext();
            string prefix = customerType switch
            {
                CustomerType.InstallmentCustomer => "CUST",
                CustomerType.BNPLCreditCustomer => "BNPL",
                CustomerType.GeneralParty => "PTY",
                _ => "TEN"
            };

            int count = db.Tenants.Count() + 1;
            string code = $"{prefix}-{count:D3}";
            while (db.Tenants.Any(t => t.TenantCode == code))
            {
                count++;
                code = $"{prefix}-{count:D3}";
            }
            return code;
        }

        public static List<Tenant> GetAllTenants(TenantStatus? statusFilter = null, string? search = null, CustomerType? customerType = null)
        {
            using var db = new AppDbContext();
            var query = db.Tenants
                          .Include(t => t.RentAgreements)
                          .ThenInclude(a => a.PropertyUnit)
                          .ThenInclude(u => u!.Property)
                          .Include(t => t.InstallmentSales)
                          .AsNoTracking()
                          .AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            if (customerType.HasValue)
            {
                query = query.Where(t => t.CustomerType == customerType.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(t =>
                    t.FullName.ToLower().Contains(s) ||
                    t.TenantCode.ToLower().Contains(s) ||
                    t.ContactNumber.Contains(s) ||
                    (t.CnicOrId != null && t.CnicOrId.Contains(s)) ||
                    (t.GuarantorName != null && t.GuarantorName.ToLower().Contains(s)) ||
                    (t.City != null && t.City.ToLower().Contains(s)) ||
                    t.RentAgreements.Any(a => a.PropertyUnit != null &&
                        (a.PropertyUnit.UnitNumber.ToLower().Contains(s) ||
                         a.PropertyUnit.Property!.Name.ToLower().Contains(s))) ||
                    t.InstallmentSales.Any(sale => sale.InvoiceNumber.ToLower().Contains(s))
                );
            }

            return query.OrderBy(t => t.FullName).ToList();
        }

        public static Tenant? GetTenantById(int tenantId)
        {
            using var db = new AppDbContext();
            return db.Tenants
                     .Include(t => t.RentAgreements)
                     .ThenInclude(a => a.PropertyUnit)
                     .ThenInclude(u => u!.Property)
                     .Include(t => t.InstallmentSales).ThenInclude(s => s.Items)
                     .Include(t => t.Transactions.Where(tr => !tr.IsVoided))
                     .FirstOrDefault(t => t.Id == tenantId);
        }

        public static (bool Success, string Message, Tenant? Tenant) SaveTenant(Tenant model)
        {
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                return (false, "Customer/Tenant name is required.", null);
            }

            if (string.IsNullOrWhiteSpace(model.ContactNumber))
            {
                return (false, "Contact number is required.", null);
            }

            using var db = new AppDbContext();

            if (string.IsNullOrWhiteSpace(model.TenantCode))
            {
                model.TenantCode = GenerateNextTenantCode(model.CustomerType);
            }

            if (model.Id == 0)
            {
                if (db.Tenants.Any(t => t.TenantCode == model.TenantCode))
                {
                    model.TenantCode = GenerateNextTenantCode(model.CustomerType);
                }

                model.CreatedAt = DateTime.Now;
                db.Tenants.Add(model);
                db.SaveChanges();

                AuditService.Log("Create Customer/Tenant", "Tenant", model.Id.ToString(), $"Created {model.CustomerType} '{model.FullName}' ({model.TenantCode})");
                return (true, "Record created successfully.", model);
            }
            else
            {
                var existing = db.Tenants.Find(model.Id);
                if (existing == null) return (false, "Record not found.", null);

                if (db.Tenants.Any(t => t.TenantCode == model.TenantCode && t.Id != model.Id))
                {
                    return (false, $"Code '{model.TenantCode}' is already in use.", null);
                }

                existing.TenantCode = model.TenantCode.Trim();
                existing.FullName = model.FullName.Trim();
                existing.FatherOrHusbandName = model.FatherOrHusbandName?.Trim();
                existing.CnicOrId = model.CnicOrId?.Trim();
                existing.ContactNumber = model.ContactNumber.Trim();
                existing.AlternateContact = model.AlternateContact?.Trim();
                existing.PermanentAddress = model.PermanentAddress?.Trim();
                existing.City = model.City?.Trim();
                existing.EmergencyContactName = model.EmergencyContactName?.Trim();
                existing.EmergencyPhone = model.EmergencyPhone?.Trim();
                existing.Status = model.Status;
                existing.CustomerType = model.CustomerType;
                existing.CreditLimit = model.CreditLimit;
                existing.GuarantorName = model.GuarantorName?.Trim();
                existing.GuarantorFatherName = model.GuarantorFatherName?.Trim();
                existing.GuarantorCnic = model.GuarantorCnic?.Trim();
                existing.GuarantorPhone = model.GuarantorPhone?.Trim();
                existing.GuarantorAddress = model.GuarantorAddress?.Trim();
                existing.GuarantorRelation = model.GuarantorRelation?.Trim();
                existing.GuarantorNotes = model.GuarantorNotes?.Trim();
                existing.Rating = model.Rating?.Trim() ?? "Good";
                existing.Notes = model.Notes?.Trim();

                db.SaveChanges();
                AuditService.Log("Update Customer/Tenant", "Tenant", existing.Id.ToString(), $"Updated {existing.CustomerType} '{existing.FullName}' ({existing.TenantCode})");
                return (true, "Record updated successfully.", existing);
            }
        }

        public static (bool Success, string Message) DeleteOrArchiveTenant(int tenantId)
        {
            using var db = new AppDbContext();
            var tenant = db.Tenants
                .Include(t => t.RentAgreements)
                .Include(t => t.InstallmentSales)
                .FirstOrDefault(t => t.Id == tenantId);

            if (tenant == null) return (false, "Record not found.");

            bool hasTransactions = db.Transactions.Any(t => t.TenantId == tenantId);
            bool hasActiveAgreements = tenant.RentAgreements.Any(a => a.Status == AgreementStatus.Active);
            bool hasSales = tenant.InstallmentSales.Any();

            if (hasTransactions || hasActiveAgreements || hasSales)
            {
                tenant.Status = TenantStatus.Previous;
                db.SaveChanges();
                AuditService.Log("Archive Customer/Tenant", "Tenant", tenant.Id.ToString(), $"Changed status of customer/tenant '{tenant.FullName}' to Previous because financial/sales records exist.");
                return (true, "Record has financial/sales history and was marked as 'Previous' instead of deleted.");
            }

            db.RentAgreements.RemoveRange(tenant.RentAgreements);
            db.Tenants.Remove(tenant);
            db.SaveChanges();

            AuditService.Log("Delete Customer/Tenant", "Tenant", tenantId.ToString(), $"Deleted record '{tenant.FullName}'.");
            return (true, "Record deleted successfully.");
        }

        public static decimal GetTenantCurrentBalance(int tenantId)
        {
            using var db = new AppDbContext();
            var nonVoidTransactions = db.Transactions
                                        .Where(t => t.TenantId == tenantId && !t.IsVoided)
                                        .ToList();

            decimal totalDebit = nonVoidTransactions.Sum(t => t.Debit);
            decimal totalCredit = nonVoidTransactions.Sum(t => t.Credit);

            return totalDebit - totalCredit;
        }
    }
}
