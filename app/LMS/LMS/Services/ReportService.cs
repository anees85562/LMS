using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class ReportDataset
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DataTable Data { get; set; } = new();
        public Dictionary<string, string> SummaryCards { get; set; } = new();
    }

    public class ReportService
    {
        public static ReportDataset GetMonthlyRentReport(int year, int month, int? propertyId = null)
        {
            string monthYear = $"{year:D4}-{month:D2}";
            var schedules = BillingService.GetRentSchedules(year, month, propertyId);

            var dt = new DataTable();
            dt.Columns.Add("Tenant Code", typeof(string));
            dt.Columns.Add("Tenant Name", typeof(string));
            dt.Columns.Add("Property", typeof(string));
            dt.Columns.Add("Unit", typeof(string));
            dt.Columns.Add("Base Rent", typeof(decimal));
            dt.Columns.Add("Utilities & Other", typeof(decimal));
            dt.Columns.Add("Total Demanded", typeof(decimal));
            dt.Columns.Add("Amount Paid", typeof(decimal));
            dt.Columns.Add("Remaining Balance", typeof(decimal));
            dt.Columns.Add("Due Date", typeof(string));
            dt.Columns.Add("Status", typeof(string));

            decimal totalBase = 0;
            decimal totalOther = 0;
            decimal totalDemanded = 0;
            decimal totalPaid = 0;
            decimal totalBalance = 0;

            foreach (var s in schedules)
            {
                decimal other = s.UtilityCharges + s.MaintenanceCharges + s.LateFee;
                dt.Rows.Add(
                    s.RentAgreement?.Tenant?.TenantCode ?? "",
                    s.RentAgreement?.Tenant?.FullName ?? "",
                    s.RentAgreement?.PropertyUnit?.Property?.Name ?? "",
                    s.RentAgreement?.PropertyUnit?.UnitNumber ?? "",
                    s.BaseRent,
                    other,
                    s.TotalDue,
                    s.AmountPaid,
                    s.Balance,
                    s.DueDate.ToString("dd/MM/yyyy"),
                    s.Status.ToString()
                );

                totalBase += s.BaseRent;
                totalOther += other;
                totalDemanded += s.TotalDue;
                totalPaid += s.AmountPaid;
                totalBalance += s.Balance;
            }

            var ds = new ReportDataset
            {
                Title = $"Monthly Rent Collection Sheet ({new DateTime(year, month, 1):MMMM yyyy})",
                Subtitle = $"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}",
                Data = dt
            };

            ds.SummaryCards["Total Expected"] = SettingService.FormatCurrency(totalDemanded);
            ds.SummaryCards["Total Received"] = SettingService.FormatCurrency(totalPaid);
            ds.SummaryCards["Pending Balance"] = SettingService.FormatCurrency(totalBalance);
            ds.SummaryCards["Collection Rate"] = totalDemanded > 0 ? $"{(totalPaid / totalDemanded * 100):N1}%" : "0.0%";

            return ds;
        }

        public static ReportDataset GetTenantStatementReport(int tenantId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var stmt = LedgerService.GetTenantLedger(tenantId, fromDate, toDate);

            var dt = new DataTable();
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Transaction #", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Ref #", typeof(string));
            dt.Columns.Add("Method", typeof(string));
            dt.Columns.Add("Debit (Charges/Sales)", typeof(decimal));
            dt.Columns.Add("Credit (Payments)", typeof(decimal));
            dt.Columns.Add("Balance", typeof(decimal));

            foreach (var e in stmt.Entries)
            {
                dt.Rows.Add(
                    e.Date.ToString("dd/MM/yyyy"),
                    e.TransactionCode,
                    e.TypeName,
                    e.Description,
                    e.Reference ?? "-",
                    e.PaymentMethod,
                    e.Debit,
                    e.Credit,
                    e.Balance
                );
            }

            string periodStr = (fromDate.HasValue || toDate.HasValue)
                ? $"Period: {(fromDate.HasValue ? fromDate.Value.ToString("dd/MM/yyyy") : "Start")} to {(toDate.HasValue ? toDate.Value.ToString("dd/MM/yyyy") : "Present")}"
                : "Complete Historical Statement";

            var ds = new ReportDataset
            {
                Title = $"Statement of Account - {stmt.Tenant?.FullName ?? "Customer"} ({stmt.Tenant?.TenantCode})",
                Subtitle = $"{periodStr} | Contact: {stmt.Tenant?.ContactNumber}",
                Data = dt
            };

            ds.SummaryCards["Opening Balance"] = SettingService.FormatCurrency(stmt.OpeningBalance);
            ds.SummaryCards["Total Charges (Debit)"] = SettingService.FormatCurrency(stmt.TotalDebit);
            ds.SummaryCards["Total Payments (Credit)"] = SettingService.FormatCurrency(stmt.TotalCredit);
            ds.SummaryCards["Closing Balance"] = SettingService.FormatCurrency(stmt.ClosingBalance);

            return ds;
        }

        public static ReportDataset GetInstallmentSalesReport(DateTime? fromDate = null, DateTime? toDate = null, SaleType? saleType = null)
        {
            using var db = new AppDbContext();
            var query = db.InstallmentSales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                .AsNoTracking()
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.SaleDate >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(s => s.SaleDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            if (saleType.HasValue)
                query = query.Where(s => s.SaleType == saleType.Value);

            var sales = query.OrderByDescending(s => s.SaleDate).ToList();

            var dt = new DataTable();
            dt.Columns.Add("Invoice #", typeof(string));
            dt.Columns.Add("Sale Date", typeof(string));
            dt.Columns.Add("Customer Name", typeof(string));
            dt.Columns.Add("Sale Type", typeof(string));
            dt.Columns.Add("Items", typeof(string));
            dt.Columns.Add("Net Price", typeof(decimal));
            dt.Columns.Add("Down Payment", typeof(decimal));
            dt.Columns.Add("Total Paid", typeof(decimal));
            dt.Columns.Add("Remaining Balance", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));

            decimal totalNet = 0;
            decimal totalDown = 0;
            decimal totalPaid = 0;
            decimal totalRemaining = 0;

            foreach (var s in sales)
            {
                string itemsStr = string.Join(", ", s.Items.Select(i => i.ItemDescription));
                dt.Rows.Add(
                    s.InvoiceNumber,
                    s.SaleDate.ToString("dd/MM/yyyy"),
                    s.Customer?.FullName ?? "",
                    s.SaleType.ToString(),
                    itemsStr,
                    s.NetSalePrice,
                    s.DownPayment,
                    s.TotalPaid,
                    s.RemainingBalance,
                    s.Status.ToString()
                );

                totalNet += s.NetSalePrice;
                totalDown += s.DownPayment;
                totalPaid += s.TotalPaid;
                totalRemaining += s.RemainingBalance;
            }

            var ds = new ReportDataset
            {
                Title = "Sales & Installment Plans Report",
                Subtitle = $"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}",
                Data = dt
            };

            ds.SummaryCards["Total Sales"] = SettingService.FormatCurrency(totalNet);
            ds.SummaryCards["Down Payments"] = SettingService.FormatCurrency(totalDown);
            ds.SummaryCards["Total Recovered"] = SettingService.FormatCurrency(totalPaid);
            ds.SummaryCards["Total Outstanding"] = SettingService.FormatCurrency(totalRemaining);

            return ds;
        }

        public static ReportDataset GetStockInventoryReport()
        {
            using var db = new AppDbContext();
            var products = db.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToList();

            var dt = new DataTable();
            dt.Columns.Add("Product Code", typeof(string));
            dt.Columns.Add("Product Name", typeof(string));
            dt.Columns.Add("Category", typeof(string));
            dt.Columns.Add("Brand / Model", typeof(string));
            dt.Columns.Add("Purchase Cost", typeof(decimal));
            dt.Columns.Add("Cash Price", typeof(decimal));
            dt.Columns.Add("Installment Price", typeof(decimal));
            dt.Columns.Add("Stock Qty", typeof(int));
            dt.Columns.Add("Min Stock", typeof(int));
            dt.Columns.Add("Stock Valuation", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));

            decimal totalValuation = 0;
            int totalUnits = 0;
            int lowStockCount = 0;

            foreach (var p in products)
            {
                decimal val = p.CurrentStock * p.PurchasePrice;
                string status = !p.TrackStock ? "Non-tracked" : (p.CurrentStock == 0 ? "Out of Stock" : (p.CurrentStock <= p.MinimumStockLevel ? "Low Stock" : "In Stock"));

                dt.Rows.Add(
                    p.ProductCode,
                    p.Name,
                    p.Category,
                    $"{p.Brand ?? ""} {p.Model ?? ""}".Trim(),
                    p.PurchasePrice,
                    p.CashSalePrice,
                    p.InstallmentSalePrice,
                    p.CurrentStock,
                    p.MinimumStockLevel,
                    val,
                    status
                );

                if (p.TrackStock)
                {
                    totalValuation += val;
                    totalUnits += p.CurrentStock;
                    if (p.CurrentStock <= p.MinimumStockLevel) lowStockCount++;
                }
            }

            var ds = new ReportDataset
            {
                Title = "Product Inventory & Stock Valuation Report",
                Subtitle = $"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}",
                Data = dt
            };

            ds.SummaryCards["Total Active Products"] = products.Count.ToString();
            ds.SummaryCards["Total Units in Stock"] = totalUnits.ToString();
            ds.SummaryCards["Inventory Valuation"] = SettingService.FormatCurrency(totalValuation);
            ds.SummaryCards["Low Stock Items"] = lowStockCount.ToString();

            return ds;
        }

        public static ReportDataset GetDefaultersReport()
        {
            var recvService = new ReceivablesService();
            var defaulters = recvService.GetDefaultersList();

            var dt = new DataTable();
            dt.Columns.Add("Party / Customer Code", typeof(string));
            dt.Columns.Add("Party / Customer Name", typeof(string));
            dt.Columns.Add("Contact Phone", typeof(string));
            dt.Columns.Add("CNIC / Ref", typeof(string));
            dt.Columns.Add("Account / Asset Ref", typeof(string));
            dt.Columns.Add("Overdue Aging Bucket", typeof(string));
            dt.Columns.Add("Days Overdue", typeof(int));
            dt.Columns.Add("Missed Installments/Rents", typeof(int));
            dt.Columns.Add("Total Outstanding", typeof(decimal));
            dt.Columns.Add("Overdue Amount", typeof(decimal));
            dt.Columns.Add("Rating", typeof(string));

            decimal totalOverdue = 0;
            decimal totalOutstanding = 0;

            foreach (var d in defaulters)
            {
                dt.Rows.Add(
                    d.CustomerCode,
                    d.FullName,
                    d.ContactNumber,
                    d.CnicOrId ?? "-",
                    d.ReferenceDetails,
                    d.Bucket,
                    d.DaysOverdue,
                    d.MissedInstallmentsCount,
                    d.TotalOutstanding,
                    d.OverdueAmount,
                    d.Rating
                );

                totalOverdue += d.OverdueAmount;
                totalOutstanding += d.TotalOutstanding;
            }

            var ds = new ReportDataset
            {
                Title = "Universal Defaulters & Overdue Receivables Aging Report",
                Subtitle = $"As of {DateTime.Now:dd/MM/yyyy}",
                Data = dt
            };

            ds.SummaryCards["Total Defaulters"] = defaulters.Count.ToString();
            ds.SummaryCards["Total Overdue Amount"] = SettingService.FormatCurrency(totalOverdue);
            ds.SummaryCards["Total Outstanding"] = SettingService.FormatCurrency(totalOutstanding);

            return ds;
        }

        public static ReportDataset GetVacancyReport()
        {
            var units = PropertyService.GetAllUnits();

            var dt = new DataTable();
            dt.Columns.Add("Property Code", typeof(string));
            dt.Columns.Add("Property Name", typeof(string));
            dt.Columns.Add("Unit Number", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Floor", typeof(string));
            dt.Columns.Add("Base Rent", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("Current Occupant", typeof(string));

            int occupied = 0;
            int vacant = 0;
            int maintenance = 0;

            foreach (var u in units)
            {
                var activeLease = u.RentAgreements.FirstOrDefault(a => a.Status == AgreementStatus.Active);
                string occupant = activeLease?.Tenant?.FullName ?? "-";

                dt.Rows.Add(
                    u.Property?.PropertyCode ?? "",
                    u.Property?.Name ?? "",
                    u.UnitNumber,
                    u.UnitType,
                    u.Floor ?? "-",
                    u.BaseRent,
                    u.Status.ToString(),
                    occupant
                );

                if (u.Status == UnitStatus.Occupied) occupied++;
                else if (u.Status == UnitStatus.Vacant) vacant++;
                else maintenance++;
            }

            var ds = new ReportDataset
            {
                Title = "Property & Unit Occupancy / Vacancy Report",
                Subtitle = $"Generated on {DateTime.Now:dd/MM/yyyy}",
                Data = dt
            };

            ds.SummaryCards["Total Units"] = units.Count.ToString();
            ds.SummaryCards["Occupied"] = occupied.ToString();
            ds.SummaryCards["Vacant"] = vacant.ToString();
            ds.SummaryCards["Under Maintenance"] = maintenance.ToString();
            ds.SummaryCards["Occupancy Rate"] = units.Count > 0 ? $"{(occupied * 100.0 / units.Count):N1}%" : "0.0%";

            return ds;
        }

        public static ReportDataset GetFinancialSummaryReport(DateTime fromDate, DateTime toDate)
        {
            using var db = new AppDbContext();
            var txs = db.Transactions
                        .Where(t => !t.IsVoided &&
                                    t.TransactionDate >= fromDate.Date &&
                                    t.TransactionDate <= toDate.Date.AddDays(1).AddTicks(-1))
                        .ToList();

            var dt = new DataTable();
            dt.Columns.Add("Category / Transaction Type", typeof(string));
            dt.Columns.Add("Transaction Count", typeof(int));
            dt.Columns.Add("Total Debit (Charges/Sales/Expenses)", typeof(decimal));
            dt.Columns.Add("Total Credit (Collections/Income)", typeof(decimal));

            var grouped = txs.GroupBy(t => t.TransactionType);

            decimal totalInc = 0;
            decimal totalExp = 0;

            foreach (var g in grouped)
            {
                decimal deb = g.Sum(t => t.Debit);
                decimal cred = g.Sum(t => t.Credit);

                dt.Rows.Add(
                    g.Key.ToString(),
                    g.Count(),
                    deb,
                    cred
                );

                totalExp += deb;
                totalInc += cred;
            }

            var ds = new ReportDataset
            {
                Title = "Financial Revenue & Expense Summary",
                Subtitle = $"From {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}",
                Data = dt
            };

            ds.SummaryCards["Total Collections (Income)"] = SettingService.FormatCurrency(totalInc);
            ds.SummaryCards["Total Debits / Charges"] = SettingService.FormatCurrency(totalExp);
            ds.SummaryCards["Net Cashflow"] = SettingService.FormatCurrency(totalInc - totalExp);

            return ds;
        }
    }
}
