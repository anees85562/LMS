using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class InstallmentSaleService
    {
        private readonly InventoryService _inventoryService;
        private readonly AuditService _auditService;
        private readonly SettingService _settingService;

        public InstallmentSaleService(
            InventoryService inventoryService,
            AuditService auditService,
            SettingService settingService)
        {
            _inventoryService = inventoryService;
            _auditService = auditService;
            _settingService = settingService;
        }

        public string GenerateNextInvoiceNumber()
        {
            using var db = new AppDbContext();
            string prefix = _settingService.GetSettingValue("Retail.InvoicePrefix", "INV");
            int year = DateTime.Now.Year;
            int count = db.InstallmentSales.Count(s => s.SaleDate.Year == year);
            return $"{prefix}-{year}-{(count + 1):D5}";
        }

        public List<InstallmentSchedule> GenerateScheduleList(
            decimal financedAmount,
            int numberOfInstallments,
            InstallmentFrequency frequency,
            DateTime firstDueDate,
            decimal customInstallmentAmount = 0)
        {
            var list = new List<InstallmentSchedule>();
            if (numberOfInstallments <= 0 || financedAmount <= 0) return list;

            decimal baseAmount = customInstallmentAmount > 0
                ? customInstallmentAmount
                : Math.Floor((financedAmount / numberOfInstallments) * 100m) / 100m;

            decimal totalAllocated = 0;
            DateTime curDate = firstDueDate;

            for (int i = 1; i <= numberOfInstallments; i++)
            {
                decimal due = (i == numberOfInstallments && customInstallmentAmount == 0)
                    ? (financedAmount - totalAllocated)
                    : baseAmount;

                totalAllocated += due;

                var schedule = new InstallmentSchedule
                {
                    InstallmentNumber = i,
                    DueDate = curDate,
                    DueAmount = due,
                    PaidAmount = 0,
                    RemainingAmount = due,
                    LateFee = 0,
                    Status = InstallmentItemStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                list.Add(schedule);

                // Calculate next date
                curDate = frequency switch
                {
                    InstallmentFrequency.Weekly => curDate.AddDays(7),
                    InstallmentFrequency.BiWeekly => curDate.AddDays(14),
                    InstallmentFrequency.Custom => curDate.AddMonths(1),
                    _ => curDate.AddMonths(1)
                };
            }

            return list;
        }

        public (bool Success, string Message, InstallmentSale? Sale) CreateInstallmentSale(
            InstallmentSale sale,
            List<SaleItem> items,
            int currentUserId,
            string currentUsername)
        {
            if (sale.CustomerId <= 0)
                return (false, "Customer is required.", null);

            if (items == null || !items.Any())
                return (false, "At least one item or product is required for the sale.", null);

            if (sale.NetSalePrice <= 0)
                return (false, "Total sale price must be greater than zero.", null);

            if (sale.DownPayment < 0 || sale.DownPayment > sale.NetSalePrice)
                return (false, "Invalid Down Payment amount.", null);

            using var db = new AppDbContext();

            var customer = db.Tenants.FirstOrDefault(t => t.Id == sale.CustomerId);
            if (customer == null)
                return (false, "Customer not found.", null);

            // Invoice number
            if (string.IsNullOrWhiteSpace(sale.InvoiceNumber))
            {
                string prefix = _settingService.GetSettingValue("Retail.InvoicePrefix", "INV");
                int year = DateTime.Now.Year;
                int count = db.InstallmentSales.Count(s => s.SaleDate.Year == year);
                sale.InvoiceNumber = $"{prefix}-{year}-{(count + 1):D5}";
            }

            decimal financed = sale.NetSalePrice - sale.DownPayment;
            sale.FinancedAmount = financed;
            sale.TotalPaid = sale.DownPayment;
            sale.RemainingBalance = financed;
            sale.Status = financed <= 0 ? InstallmentPlanStatus.Completed : InstallmentPlanStatus.Active;
            sale.CreatedByUserId = currentUserId;
            sale.CreatedAt = DateTime.Now;

            // Generate Schedules if installment/BNPL
            if (sale.NumberOfInstallments > 0 && financed > 0)
            {
                var schedules = GenerateScheduleList(
                    financed,
                    sale.NumberOfInstallments,
                    sale.Frequency,
                    sale.FirstDueDate,
                    sale.InstallmentAmount > 0 && sale.InstallmentAmount * sale.NumberOfInstallments == financed ? sale.InstallmentAmount : 0
                );

                foreach (var sch in schedules)
                {
                    sale.Schedules.Add(sch);
                }
            }

            // Add Sale Items
            foreach (var item in items)
            {
                sale.Items.Add(item);
            }

            db.InstallmentSales.Add(sale);
            db.SaveChanges(); // Save to generate Sale.Id

            // 1. Record Debit Transaction (Sale Invoice)
            string transCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var invoiceTx = new Transaction
            {
                TransactionCode = transCode,
                TransactionDate = sale.SaleDate,
                TransactionType = sale.SaleType switch
                {
                    SaleType.BNPLSale => TransactionType.BNPLCharge,
                    SaleType.CreditSale => TransactionType.CreditSale,
                    _ => TransactionType.SaleInvoice
                },
                TenantId = customer.Id,
                InstallmentSaleId = sale.Id,
                Debit = sale.NetSalePrice,
                Credit = 0,
                RunningBalance = sale.NetSalePrice,
                ReferenceNumber = sale.InvoiceNumber,
                Description = $"{sale.SaleType} Invoice #{sale.InvoiceNumber} - {string.Join(", ", items.Select(i => i.ItemDescription))}",
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.Now
            };
            db.Transactions.Add(invoiceTx);
            db.SaveChanges();

            // 2. If Down Payment > 0, record Credit Transaction & Receipt
            if (sale.DownPayment > 0)
            {
                string dpTransCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                var dpTx = new Transaction
                {
                    TransactionCode = dpTransCode,
                    TransactionDate = sale.SaleDate,
                    TransactionType = TransactionType.DownPayment,
                    TenantId = customer.Id,
                    InstallmentSaleId = sale.Id,
                    Debit = 0,
                    Credit = sale.DownPayment,
                    RunningBalance = sale.RemainingBalance,
                    PaymentMethod = PaymentMethod.Cash,
                    ReferenceNumber = sale.InvoiceNumber,
                    Description = $"Down Payment for Invoice #{sale.InvoiceNumber}",
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.Now
                };
                db.Transactions.Add(dpTx);
                db.SaveChanges();

                // Payment Receipt for Down Payment
                string rcpPrefix = _settingService.GetSettingValue("Receipt.Prefix", "RCP");
                int rcpCount = db.PaymentReceipts.Count();
                string rcpNum = $"{rcpPrefix}-{DateTime.Now.Year}-{(rcpCount + 1):D5}";

                var receipt = new PaymentReceipt
                {
                    ReceiptNumber = rcpNum,
                    TransactionId = dpTx.Id,
                    TenantId = customer.Id,
                    InstallmentSaleId = sale.Id,
                    InvoiceNumber = sale.InvoiceNumber,
                    PaymentDate = sale.SaleDate,
                    AmountPaid = sale.DownPayment,
                    PaymentMethod = PaymentMethod.Cash,
                    ReferenceNumber = sale.InvoiceNumber,
                    RentalPeriod = "Down Payment",
                    PreviousBalance = sale.NetSalePrice,
                    CurrentPayment = sale.DownPayment,
                    RemainingBalance = sale.RemainingBalance,
                    ReceivedByUserId = currentUserId,
                    ReceivedByUserName = currentUsername,
                    Remarks = $"Down Payment on Invoice {sale.InvoiceNumber}",
                    CreatedAt = DateTime.Now
                };
                db.PaymentReceipts.Add(receipt);
                db.SaveChanges();
            }

            // 3. Deduct stock for inventory items
            foreach (var item in items)
            {
                if (item.ProductId.HasValue && item.ProductId.Value > 0)
                {
                    _inventoryService.RecordSaleDeduction(item.ProductId.Value, item.Quantity, sale.InvoiceNumber, currentUserId, currentUsername);
                }
            }

            _auditService.Log(currentUserId, currentUsername, "CREATE", "InstallmentSale", sale.Id.ToString(), $"Created {sale.SaleType} #{sale.InvoiceNumber} for customer '{customer.FullName}', Amount: {sale.NetSalePrice:N2}, Down Payment: {sale.DownPayment:N2}");

            return (true, $"Sale #{sale.InvoiceNumber} created successfully.", sale);
        }

        public (bool Success, string Message, PaymentReceipt? Receipt) CollectInstallmentPayment(
            int installmentSaleId,
            decimal amountPaid,
            PaymentMethod method,
            string? referenceNumber,
            string? remarks,
            int receivedByUserId,
            string receivedByUserName,
            int? specificScheduleId = null)
        {
            if (amountPaid <= 0) return (false, "Payment amount must be greater than zero.", null);

            using var db = new AppDbContext();
            var sale = db.InstallmentSales
                .Include(s => s.Customer)
                .Include(s => s.Schedules)
                .FirstOrDefault(s => s.Id == installmentSaleId);

            if (sale == null) return (false, "Installment sale record not found.", null);

            decimal prevRemainingBalance = sale.RemainingBalance;
            decimal remainingPayment = amountPaid;
            var paidScheduleNumbers = new List<int>();

            // Allocate across schedules
            if (specificScheduleId.HasValue && specificScheduleId.Value > 0)
            {
                var target = sale.Schedules.FirstOrDefault(s => s.Id == specificScheduleId.Value);
                if (target != null && target.RemainingAmount > 0)
                {
                    decimal toApply = Math.Min(remainingPayment, target.RemainingAmount);
                    target.PaidAmount += toApply;
                    target.RemainingAmount -= toApply;
                    target.PaidDate = DateTime.Now;
                    target.Status = target.RemainingAmount <= 0 ? InstallmentItemStatus.Paid : InstallmentItemStatus.Partial;
                    remainingPayment -= toApply;
                    paidScheduleNumbers.Add(target.InstallmentNumber);
                }
            }

            if (remainingPayment > 0)
            {
                // Cascade through unpaid/partial schedules ordered by DueDate/InstallmentNumber
                var unpaidSchedules = sale.Schedules
                    .Where(s => s.Status != InstallmentItemStatus.Paid && s.RemainingAmount > 0)
                    .OrderBy(s => s.InstallmentNumber)
                    .ToList();

                foreach (var sch in unpaidSchedules)
                {
                    if (remainingPayment <= 0) break;

                    decimal toApply = Math.Min(remainingPayment, sch.RemainingAmount);
                    sch.PaidAmount += toApply;
                    sch.RemainingAmount -= toApply;
                    sch.PaidDate = DateTime.Now;
                    sch.Status = sch.RemainingAmount <= 0 ? InstallmentItemStatus.Paid : InstallmentItemStatus.Partial;
                    remainingPayment -= toApply;
                    if (!paidScheduleNumbers.Contains(sch.InstallmentNumber))
                    {
                        paidScheduleNumbers.Add(sch.InstallmentNumber);
                    }
                }
            }

            // Update Master Sale
            sale.TotalPaid += amountPaid;
            sale.RemainingBalance = Math.Max(0, sale.RemainingBalance - amountPaid);
            if (sale.RemainingBalance <= 0)
            {
                sale.Status = InstallmentPlanStatus.Completed;
            }
            else
            {
                sale.Status = InstallmentPlanStatus.PartiallyPaid;
            }

            // Next Due Date
            var nextDueSchedule = sale.Schedules
                .Where(s => s.Status != InstallmentItemStatus.Paid)
                .OrderBy(s => s.DueDate)
                .FirstOrDefault();

            // Create Transaction
            string transCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            string schPeriodDesc = paidScheduleNumbers.Any()
                ? $"Installment #{string.Join(", #", paidScheduleNumbers)}"
                : "Installment Payment";

            var trans = new Transaction
            {
                TransactionCode = transCode,
                TransactionDate = DateTime.Now,
                TransactionType = TransactionType.InstallmentPayment,
                TenantId = sale.CustomerId,
                InstallmentSaleId = sale.Id,
                Debit = 0,
                Credit = amountPaid,
                RunningBalance = sale.RemainingBalance,
                PaymentMethod = method,
                ReferenceNumber = referenceNumber ?? sale.InvoiceNumber,
                Description = $"Payment of {amountPaid:N2} on Invoice #{sale.InvoiceNumber} ({schPeriodDesc})",
                Remarks = remarks,
                CreatedByUserId = receivedByUserId,
                CreatedAt = DateTime.Now
            };
            db.Transactions.Add(trans);
            db.SaveChanges();

            // Create Receipt
            string rcpPrefix = _settingService.GetSettingValue("Receipt.Prefix", "RCP");
            int rcpCount = db.PaymentReceipts.Count();
            string rcpNum = $"{rcpPrefix}-{DateTime.Now.Year}-{(rcpCount + 1):D5}";

            var receipt = new PaymentReceipt
            {
                ReceiptNumber = rcpNum,
                TransactionId = trans.Id,
                TenantId = sale.CustomerId,
                InstallmentSaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                PaymentDate = DateTime.Now,
                AmountPaid = amountPaid,
                PaymentMethod = method,
                ReferenceNumber = referenceNumber,
                RentalPeriod = schPeriodDesc,
                NextDueDate = nextDueSchedule?.DueDate,
                PreviousBalance = prevRemainingBalance,
                CurrentPayment = amountPaid,
                RemainingBalance = sale.RemainingBalance,
                ReceivedByUserId = receivedByUserId,
                ReceivedByUserName = receivedByUserName,
                Remarks = remarks,
                CreatedAt = DateTime.Now
            };

            db.PaymentReceipts.Add(receipt);
            db.SaveChanges();

            _auditService.Log(receivedByUserId, receivedByUserName, "COLLECT_PAYMENT", "InstallmentSale", sale.Id.ToString(), $"Collected {amountPaid:N2} on Invoice #{sale.InvoiceNumber}. New balance: {sale.RemainingBalance:N2}");

            return (true, $"Payment of {amountPaid:N2} recorded successfully (Receipt #{rcpNum}).", receipt);
        }

        public (bool Success, string Message) EarlySettleSale(
            int saleId,
            decimal settlementAmount,
            decimal discountAmount,
            PaymentMethod method,
            string? remarks,
            int userId,
            string username)
        {
            using var db = new AppDbContext();
            var sale = db.InstallmentSales
                .Include(s => s.Customer)
                .Include(s => s.Schedules)
                .FirstOrDefault(s => s.Id == saleId);

            if (sale == null) return (false, "Sale not found.");
            if (sale.RemainingBalance <= 0) return (false, "Sale is already fully paid.");

            decimal totalSettling = settlementAmount + discountAmount;
            if (totalSettling < sale.RemainingBalance)
            {
                return (false, "Settlement amount plus discount does not cover the remaining balance.");
            }

            // 1. Payment transaction
            if (settlementAmount > 0)
            {
                string transCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                var trans = new Transaction
                {
                    TransactionCode = transCode,
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.InstallmentPayment,
                    TenantId = sale.CustomerId,
                    InstallmentSaleId = sale.Id,
                    Debit = 0,
                    Credit = settlementAmount,
                    RunningBalance = 0,
                    PaymentMethod = method,
                    ReferenceNumber = sale.InvoiceNumber,
                    Description = $"Early Settlement Payment on Invoice #{sale.InvoiceNumber}",
                    Remarks = remarks,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.Now
                };
                db.Transactions.Add(trans);
            }

            // 2. Early Settlement Discount transaction
            if (discountAmount > 0)
            {
                string discTransCode = $"TRX-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
                var discTrans = new Transaction
                {
                    TransactionCode = discTransCode,
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.EarlySettlementDiscount,
                    TenantId = sale.CustomerId,
                    InstallmentSaleId = sale.Id,
                    Debit = 0,
                    Credit = discountAmount,
                    RunningBalance = 0,
                    PaymentMethod = PaymentMethod.Other,
                    ReferenceNumber = sale.InvoiceNumber,
                    Description = $"Early Settlement Discount on Invoice #{sale.InvoiceNumber}",
                    Remarks = remarks,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.Now
                };
                db.Transactions.Add(discTrans);
            }

            // Mark all schedules as paid
            foreach (var sch in sale.Schedules.Where(s => s.Status != InstallmentItemStatus.Paid))
            {
                sch.PaidAmount = sch.DueAmount;
                sch.RemainingAmount = 0;
                sch.PaidDate = DateTime.Now;
                sch.Status = InstallmentItemStatus.Paid;
            }

            sale.TotalPaid += settlementAmount;
            sale.Discount += discountAmount;
            sale.RemainingBalance = 0;
            sale.Status = InstallmentPlanStatus.Settled;

            db.SaveChanges();

            _auditService.Log(userId, username, "EARLY_SETTLEMENT", "InstallmentSale", sale.Id.ToString(), $"Early settled Invoice #{sale.InvoiceNumber} with payment {settlementAmount:N2} and discount {discountAmount:N2}");

            return (true, $"Sale #{sale.InvoiceNumber} early-settled successfully.");
        }

        public InstallmentSale? GetInstallmentSaleById(int saleId)
        {
            using var db = new AppDbContext();
            return db.InstallmentSales
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Schedules)
                .Include(s => s.Transactions)
                .FirstOrDefault(s => s.Id == saleId);
        }

        public List<InstallmentSale> GetInstallmentSales(
            string? search = null,
            SaleType? saleType = null,
            InstallmentPlanStatus? status = null,
            int? customerId = null)
        {
            using var db = new AppDbContext();
            var query = db.InstallmentSales
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(i => i.Product)
                .Include(s => s.Schedules)
                .AsNoTracking()
                .AsQueryable();

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(s => s.CustomerId == customerId.Value);
            }

            if (saleType.HasValue)
            {
                query = query.Where(s => s.SaleType == saleType.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(sale =>
                    sale.InvoiceNumber.ToLower().Contains(s) ||
                    (sale.Customer != null && (sale.Customer.FullName.ToLower().Contains(s) || sale.Customer.ContactNumber.Contains(s) || (sale.Customer.CnicOrId != null && sale.Customer.CnicOrId.Contains(s)))) ||
                    sale.Items.Any(i => i.ItemDescription.ToLower().Contains(s) || (i.SerialNumber != null && i.SerialNumber.ToLower().Contains(s)))
                );
            }

            return query.OrderByDescending(s => s.SaleDate).ToList();
        }
    }
}
