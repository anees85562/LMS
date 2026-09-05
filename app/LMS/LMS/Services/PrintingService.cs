using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;

namespace LMS.Services
{
    public class PrintingService
    {
        public static void PrintReceipt(PaymentReceipt receipt, bool isReprint = false, bool showPreview = true)
        {
            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"Receipt_{receipt.ReceiptNumber}";
            printDoc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

            printDoc.PrintPage += (sender, e) =>
            {
                var g = e.Graphics;
                if (g == null) return;

                int left = e.MarginBounds.Left;
                int top = e.MarginBounds.Top;
                int width = e.MarginBounds.Width;

                using var fontHeader = new Font("Segoe UI", 16, FontStyle.Bold);
                using var fontSubHeader = new Font("Segoe UI", 10, FontStyle.Regular);
                using var fontTitle = new Font("Segoe UI", 13, FontStyle.Bold);
                using var fontBold = new Font("Segoe UI", 10, FontStyle.Bold);
                using var fontRegular = new Font("Segoe UI", 10, FontStyle.Regular);
                using var fontSmall = new Font("Segoe UI", 8, FontStyle.Italic);
                using var brushBlack = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var brushGray = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var penBorder = new Pen(Color.FromArgb(203, 213, 225), 1);
                using var penThick = new Pen(Color.FromArgb(37, 99, 235), 2);

                int currentY = top;

                // Company Header
                string companyName = SettingService.Get("General.CompanyName", "Installment & Receivables Management");
                string address = SettingService.Get("General.Address", "Main Commercial Plaza, City");
                string phone = SettingService.Get("General.Phone", "+92 300 0000000");

                var headerSize = g.MeasureString(companyName, fontHeader);
                g.DrawString(companyName, fontHeader, brushBlack, left + (width - headerSize.Width) / 2, currentY);
                currentY += (int)headerSize.Height + 2;

                string sub = $"{address} | Phone: {phone}";
                var subSize = g.MeasureString(sub, fontSubHeader);
                g.DrawString(sub, fontSubHeader, brushGray, left + (width - subSize.Width) / 2, currentY);
                currentY += (int)subSize.Height + 8;

                g.DrawLine(penThick, left, currentY, left + width, currentY);
                currentY += 12;

                // Reprint Watermark / Banner
                if (isReprint)
                {
                    using var bannerBrush = new SolidBrush(Color.FromArgb(254, 242, 242));
                    using var bannerPen = new Pen(Color.FromArgb(239, 68, 68), 1);
                    using var bannerTextBrush = new SolidBrush(Color.FromArgb(220, 38, 38));

                    g.FillRectangle(bannerBrush, left, currentY, width, 24);
                    g.DrawRectangle(bannerPen, left, currentY, width, 24);
                    g.DrawString("*** DUPLICATE / REPRINT RECEIPT ***", fontBold, bannerTextBrush, left + width / 2 - 120, currentY + 3);
                    currentY += 32;
                }

                bool isRetailOrInstallment = receipt.InstallmentSaleId.HasValue || receipt.PropertyUnitId == null;

                // Title & Receipt # Bar
                string title = isRetailOrInstallment ? "PAYMENT & ACCOUNT RECEIPT" : "RENT & ACCOUNT PAYMENT RECEIPT";
                g.DrawString(title, fontTitle, brushBlack, left, currentY);

                string receiptNoStr = $"Receipt #: {receipt.ReceiptNumber}";
                var recSize = g.MeasureString(receiptNoStr, fontBold);
                g.DrawString(receiptNoStr, fontBold, brushBlack, left + width - recSize.Width, currentY);
                currentY += 28;

                // Meta table box
                int boxHeight = 110;
                using var boxBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
                g.FillRectangle(boxBrush, left, currentY, width, boxHeight);
                g.DrawRectangle(penBorder, left, currentY, width, boxHeight);

                int col1 = left + 15;
                int col2 = left + width / 2 + 10;
                int rowY = currentY + 10;

                string customerLabel = isRetailOrInstallment ? "Customer Name:" : "Tenant Name:";
                string itemLabel = isRetailOrInstallment ? "Invoice / Ref:" : "Property / Unit:";
                string periodLabel = isRetailOrInstallment ? "Payment For:" : "Rental Period:";

                string partyName = receipt.Tenant?.FullName ?? "Customer";
                string partyCode = receipt.Tenant?.TenantCode ?? "";
                string contact = receipt.Tenant?.ContactNumber ?? "";
                string itemVal = isRetailOrInstallment
                    ? (!string.IsNullOrWhiteSpace(receipt.InvoiceNumber) ? $"Invoice #{receipt.InvoiceNumber}" : "-")
                    : $"{receipt.PropertyUnit?.Property?.Name ?? "Property"} - {receipt.PropertyUnit?.UnitNumber ?? "Unit"}";

                g.DrawString(customerLabel, fontBold, brushBlack, col1, rowY);
                g.DrawString($"{partyName} ({partyCode})", fontRegular, brushBlack, col1 + 120, rowY);

                g.DrawString("Date:", fontBold, brushBlack, col2, rowY);
                g.DrawString(receipt.PaymentDate.ToString("dd/MM/yyyy"), fontRegular, brushBlack, col2 + 70, rowY);

                rowY += 24;
                g.DrawString(itemLabel, fontBold, brushBlack, col1, rowY);
                g.DrawString(itemVal, fontRegular, brushBlack, col1 + 120, rowY);

                g.DrawString(periodLabel, fontBold, brushBlack, col2, rowY);
                g.DrawString(receipt.RentalPeriod ?? "-", fontRegular, brushBlack, col2 + 95, rowY);

                rowY += 24;
                g.DrawString("Contact Phone:", fontBold, brushBlack, col1, rowY);
                g.DrawString(contact, fontRegular, brushBlack, col1 + 120, rowY);

                g.DrawString("Payment Mode:", fontBold, brushBlack, col2, rowY);
                g.DrawString(receipt.PaymentMethod.ToString(), fontRegular, brushBlack, col2 + 105, rowY);

                rowY += 24;
                if (!string.IsNullOrWhiteSpace(receipt.ReferenceNumber) || !string.IsNullOrWhiteSpace(receipt.BankName))
                {
                    g.DrawString("Ref / Cheque #:", fontBold, brushBlack, col1, rowY);
                    g.DrawString($"{receipt.ReferenceNumber ?? ""} {receipt.BankName ?? ""}".Trim(), fontRegular, brushBlack, col1 + 120, rowY);
                }

                currentY += boxHeight + 20;

                // Payment Breakdown Table
                int tableTop = currentY;
                int thHeight = 28;
                using var thBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
                g.FillRectangle(thBrush, left, tableTop, width, thHeight);
                g.DrawRectangle(penBorder, left, tableTop, width, thHeight);

                g.DrawString("Description", fontBold, brushBlack, left + 15, tableTop + 5);
                g.DrawString("Amount", fontBold, brushBlack, left + width - 120, tableTop + 5);

                currentY += thHeight;

                // Row 1: Previous Balance
                DrawReceiptRow(g, penBorder, brushBlack, fontRegular, left, currentY, width, 26, "Previous Outstanding Balance", SettingService.FormatCurrency(receipt.PreviousBalance));
                currentY += 26;

                // Row 2: Current Payment (Highlighted)
                using var payRowBrush = new SolidBrush(Color.FromArgb(239, 246, 255));
                g.FillRectangle(payRowBrush, left, currentY, width, 32);
                DrawReceiptRow(g, penBorder, brushBlack, fontBold, left, currentY, width, 32, "CURRENT AMOUNT PAID", SettingService.FormatCurrency(receipt.CurrentPayment), true);
                currentY += 32;

                // Row 3: Remaining Balance
                DrawReceiptRow(g, penBorder, brushBlack, fontBold, left, currentY, width, 28, "Remaining Account Balance", SettingService.FormatCurrency(receipt.RemainingBalance));
                currentY += 28 + 20;

                // Remarks
                if (!string.IsNullOrWhiteSpace(receipt.Remarks))
                {
                    g.DrawString($"Remarks: {receipt.Remarks}", fontRegular, brushGray, left, currentY);
                    currentY += 20;
                }

                // Signatures
                currentY += 40;
                int sigCol1 = left + 20;
                int sigCol2 = left + width - 200;

                g.DrawLine(penBorder, sigCol1, currentY, sigCol1 + 160, currentY);
                g.DrawLine(penBorder, sigCol2, currentY, sigCol2 + 160, currentY);

                g.DrawString(isRetailOrInstallment ? "Customer Signature" : "Tenant Signature", fontRegular, brushGray, sigCol1 + 25, currentY + 5);
                g.DrawString("Authorized Receiver", fontRegular, brushGray, sigCol2 + 20, currentY + 5);

                currentY += 35;
                string receiverName = !string.IsNullOrWhiteSpace(receipt.ReceivedByUserName) ? receipt.ReceivedByUserName : "Staff";
                g.DrawString($"Received by: {receiverName}", fontSmall, brushGray, sigCol2 + 20, currentY);

                // Footer Note
                currentY += 30;
                string footer = SettingService.Get("Receipt.FooterNote", "Thank you for your timely payment. Computer generated receipt.");
                var fSize = g.MeasureString(footer, fontSmall);
                g.DrawString(footer, fontSmall, brushGray, left + (width - fSize.Width) / 2, currentY);

                e.HasMorePages = false;
            };

            if (showPreview)
            {
                using var previewDlg = new PrintPreviewDialog();
                previewDlg.Document = printDoc;
                previewDlg.Width = 850;
                previewDlg.Height = 700;
                previewDlg.StartPosition = FormStartPosition.CenterScreen;
                previewDlg.ShowDialog();
            }
            else
            {
                printDoc.Print();
            }
        }

        public static void PrintInvoice(InstallmentSale sale, bool isReprint = false, bool showPreview = true)
        {
            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"Invoice_{sale.InvoiceNumber}";
            printDoc.DefaultPageSettings.Margins = new Margins(35, 35, 35, 35);

            printDoc.PrintPage += (sender, e) =>
            {
                var g = e.Graphics;
                if (g == null) return;

                int left = e.MarginBounds.Left;
                int top = e.MarginBounds.Top;
                int width = e.MarginBounds.Width;

                using var fontHeader = new Font("Segoe UI", 16, FontStyle.Bold);
                using var fontSubHeader = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                using var fontTitle = new Font("Segoe UI", 12, FontStyle.Bold);
                using var fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var fontRegular = new Font("Segoe UI", 9, FontStyle.Regular);
                using var fontSmall = new Font("Segoe UI", 8, FontStyle.Italic);
                using var brushBlack = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var brushGray = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var penBorder = new Pen(Color.FromArgb(203, 213, 225), 1);
                using var penThick = new Pen(Color.FromArgb(15, 118, 110), 2);

                int currentY = top;

                // Company Header
                string companyName = SettingService.Get("General.CompanyName", "Easy Installment & Receivables");
                string address = SettingService.Get("General.Address", "Main Commercial Plaza, City");
                string phone = SettingService.Get("General.Phone", "+92 300 0000000");

                var headerSize = g.MeasureString(companyName, fontHeader);
                g.DrawString(companyName, fontHeader, brushBlack, left + (width - headerSize.Width) / 2, currentY);
                currentY += (int)headerSize.Height + 2;

                string sub = $"{address} | Phone: {phone}";
                var subSize = g.MeasureString(sub, fontSubHeader);
                g.DrawString(sub, fontSubHeader, brushGray, left + (width - subSize.Width) / 2, currentY);
                currentY += (int)subSize.Height + 6;

                g.DrawLine(penThick, left, currentY, left + width, currentY);
                currentY += 10;

                // Invoice title bar
                string title = $"{sale.SaleType.ToString().ToUpper()} INVOICE & AGREEMENT";
                g.DrawString(title, fontTitle, brushBlack, left, currentY);
                string invNumStr = $"Invoice #: {sale.InvoiceNumber}";
                var invSize = g.MeasureString(invNumStr, fontBold);
                g.DrawString(invNumStr, fontBold, brushBlack, left + width - invSize.Width, currentY);
                currentY += 24;

                // Customer & Guarantor Details Box
                int custBoxHeight = 85;
                using var boxBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
                g.FillRectangle(boxBrush, left, currentY, width, custBoxHeight);
                g.DrawRectangle(penBorder, left, currentY, width, custBoxHeight);

                int c1 = left + 10;
                int c2 = left + width / 2 + 10;
                int rY = currentY + 8;

                g.DrawString("Customer:", fontBold, brushBlack, c1, rY);
                g.DrawString($"{sale.Customer?.FullName} ({sale.Customer?.TenantCode})", fontRegular, brushBlack, c1 + 80, rY);
                g.DrawString("Sale Date:", fontBold, brushBlack, c2, rY);
                g.DrawString(sale.SaleDate.ToString("dd/MM/yyyy"), fontRegular, brushBlack, c2 + 80, rY);

                rY += 20;
                g.DrawString("Phone / CNIC:", fontBold, brushBlack, c1, rY);
                g.DrawString($"{sale.Customer?.ContactNumber} | {sale.Customer?.CnicOrId ?? "-"}", fontRegular, brushBlack, c1 + 80, rY);
                g.DrawString("Status:", fontBold, brushBlack, c2, rY);
                g.DrawString(sale.Status.ToString(), fontBold, brushBlack, c2 + 80, rY);

                rY += 20;
                g.DrawString("Address:", fontBold, brushBlack, c1, rY);
                g.DrawString($"{sale.Customer?.PermanentAddress ?? sale.Customer?.City ?? "-"}", fontRegular, brushBlack, c1 + 80, rY);
                if (!string.IsNullOrWhiteSpace(sale.GuarantorName))
                {
                    g.DrawString("Guarantor:", fontBold, brushBlack, c2, rY);
                    g.DrawString($"{sale.GuarantorName} ({sale.GuarantorPhone})", fontRegular, brushBlack, c2 + 80, rY);
                }

                currentY += custBoxHeight + 12;

                // Items Table
                int thHeight = 24;
                using var thBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
                g.FillRectangle(thBrush, left, currentY, width, thHeight);
                g.DrawRectangle(penBorder, left, currentY, width, thHeight);

                g.DrawString("Item / Product Description", fontBold, brushBlack, left + 8, currentY + 4);
                g.DrawString("Serial / Model", fontBold, brushBlack, left + width - 330, currentY + 4);
                g.DrawString("Qty", fontBold, brushBlack, left + width - 180, currentY + 4);
                g.DrawString("Price", fontBold, brushBlack, left + width - 90, currentY + 4);

                currentY += thHeight;

                foreach (var item in sale.Items)
                {
                    int rowH = 22;
                    g.DrawRectangle(penBorder, left, currentY, width, rowH);
                    g.DrawString(item.ItemDescription, fontRegular, brushBlack, left + 8, currentY + 4);
                    g.DrawString(item.SerialNumber ?? "-", fontRegular, brushBlack, left + width - 330, currentY + 4);
                    g.DrawString(item.Quantity.ToString(), fontRegular, brushBlack, left + width - 180, currentY + 4);
                    g.DrawString(SettingService.FormatCurrency(item.TotalPrice), fontRegular, brushBlack, left + width - 110, currentY + 4);
                    currentY += rowH;
                }

                currentY += 12;

                // Summary and Plan Details
                int sumBoxH = 80;
                g.FillRectangle(boxBrush, left, currentY, width, sumBoxH);
                g.DrawRectangle(penBorder, left, currentY, width, sumBoxH);

                int sR1 = currentY + 8;
                g.DrawString("Total Sale Price:", fontBold, brushBlack, c1, sR1);
                g.DrawString(SettingService.FormatCurrency(sale.NetSalePrice), fontBold, brushBlack, c1 + 130, sR1);
                g.DrawString("Installments:", fontBold, brushBlack, c2, sR1);
                g.DrawString($"{sale.NumberOfInstallments} ({sale.Frequency})", fontRegular, brushBlack, c2 + 130, sR1);

                sR1 += 22;
                g.DrawString("Down Payment:", fontBold, brushBlack, c1, sR1);
                g.DrawString(SettingService.FormatCurrency(sale.DownPayment), fontRegular, brushBlack, c1 + 130, sR1);
                g.DrawString("Installment Amt:", fontBold, brushBlack, c2, sR1);
                g.DrawString(SettingService.FormatCurrency(sale.InstallmentAmount), fontBold, brushBlack, c2 + 130, sR1);

                sR1 += 22;
                g.DrawString("Financed / Balance:", fontBold, brushBlack, c1, sR1);
                g.DrawString(SettingService.FormatCurrency(sale.RemainingBalance), fontBold, brushBlack, c1 + 130, sR1);
                g.DrawString("First Due Date:", fontBold, brushBlack, c2, sR1);
                g.DrawString(sale.FirstDueDate.ToString("dd/MM/yyyy"), fontRegular, brushBlack, c2 + 130, sR1);

                currentY += sumBoxH + 12;

                // Installment Schedule Grid (Compact 3 columns)
                if (sale.Schedules.Any())
                {
                    g.DrawString("Payment Schedule:", fontBold, brushBlack, left, currentY);
                    currentY += 18;

                    int schColWidth = width / 3;
                    int schRowH = 18;
                    int count = 0;

                    foreach (var sch in sale.Schedules.OrderBy(s => s.InstallmentNumber))
                    {
                        int colIdx = count % 3;
                        int schX = left + colIdx * schColWidth;
                        int schY = currentY + (count / 3) * schRowH;

                        string statusText = sch.Status == InstallmentItemStatus.Paid ? "[PAID]" : $"Due: {sch.DueDate:dd/MM/yy}";
                        string text = $"#{sch.InstallmentNumber}: {SettingService.FormatCurrency(sch.DueAmount)} ({statusText})";

                        using var schFont = new Font("Segoe UI", 7.8f, sch.Status == InstallmentItemStatus.Paid ? FontStyle.Strikeout : FontStyle.Regular);
                        g.DrawString(text, schFont, brushBlack, schX, schY);

                        count++;
                    }

                    int totalSchRows = (int)Math.Ceiling(sale.Schedules.Count / 3.0);
                    currentY += totalSchRows * schRowH + 15;
                }

                // Terms and Conditions
                if (!string.IsNullOrWhiteSpace(sale.TermsAndConditions))
                {
                    g.DrawString($"Terms: {sale.TermsAndConditions}", fontSmall, brushGray, left, currentY);
                    currentY += 20;
                }

                // Signatures
                currentY += 25;
                int sig1 = left + 10;
                int sig2 = left + width / 3 + 10;
                int sig3 = left + (2 * width) / 3 + 10;

                g.DrawLine(penBorder, sig1, currentY, sig1 + 140, currentY);
                g.DrawLine(penBorder, sig2, currentY, sig2 + 140, currentY);
                g.DrawLine(penBorder, sig3, currentY, sig3 + 140, currentY);

                g.DrawString("Customer Signature", fontRegular, brushGray, sig1 + 10, currentY + 4);
                g.DrawString("Guarantor Signature", fontRegular, brushGray, sig2 + 10, currentY + 4);
                g.DrawString("Authorized Seller", fontRegular, brushGray, sig3 + 15, currentY + 4);

                e.HasMorePages = false;
            };

            if (showPreview)
            {
                using var previewDlg = new PrintPreviewDialog();
                previewDlg.Document = printDoc;
                previewDlg.Width = 900;
                previewDlg.Height = 750;
                previewDlg.StartPosition = FormStartPosition.CenterScreen;
                previewDlg.ShowDialog();
            }
            else
            {
                printDoc.Print();
            }
        }

        private static void DrawReceiptRow(Graphics g, Pen borderPen, Brush textBrush, Font font, int left, int top, int width, int height, string label, string val, bool isHighlight = false)
        {
            g.DrawRectangle(borderPen, left, top, width, height);
            g.DrawString(label, font, textBrush, left + 15, top + (height - font.Height) / 2);

            var valSize = g.MeasureString(val, font);
            g.DrawString(val, font, textBrush, left + width - valSize.Width - 15, top + (height - font.Height) / 2);
        }

        public static void PrintReport(ReportDataset dataset, bool showPreview = true)
        {
            var printDoc = new PrintDocument();
            printDoc.DocumentName = dataset.Title;
            printDoc.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);
            printDoc.DefaultPageSettings.Landscape = dataset.Data.Columns.Count > 6;

            int currentRowIndex = 0;
            int pageNumber = 1;

            printDoc.PrintPage += (sender, e) =>
            {
                var g = e.Graphics;
                if (g == null) return;

                int left = e.MarginBounds.Left;
                int top = e.MarginBounds.Top;
                int width = e.MarginBounds.Width;
                int height = e.MarginBounds.Height;
                int bottom = top + height;

                using var fontHeader = new Font("Segoe UI", 14, FontStyle.Bold);
                using var fontSub = new Font("Segoe UI", 9, FontStyle.Regular);
                using var fontTh = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var fontRow = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                using var fontSmall = new Font("Segoe UI", 8, FontStyle.Italic);
                using var brushBlack = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var brushGray = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var penBorder = new Pen(Color.FromArgb(226, 232, 240), 1);
                using var thBrush = new SolidBrush(Color.FromArgb(241, 245, 249));

                int currentY = top;

                // Header on First Page
                if (pageNumber == 1)
                {
                    string comp = SettingService.Get("General.CompanyName", "Installment & Receivables Management System");
                    g.DrawString(comp, fontSub, brushGray, left, currentY);
                    currentY += 16;

                    g.DrawString(dataset.Title, fontHeader, brushBlack, left, currentY);
                    currentY += 24;

                    g.DrawString(dataset.Subtitle, fontSub, brushGray, left, currentY);
                    currentY += 20;

                    // Summary KPI chips
                    if (dataset.SummaryCards.Count > 0)
                    {
                        int cardX = left;
                        foreach (var kvp in dataset.SummaryCards)
                        {
                            string text = $"{kvp.Key}: {kvp.Value}";
                            var sz = g.MeasureString(text, fontTh);
                            int cardWidth = (int)sz.Width + 16;

                            using var cardBg = new SolidBrush(Color.FromArgb(243, 244, 246));
                            g.FillRectangle(cardBg, cardX, currentY, cardWidth, 22);
                            g.DrawRectangle(penBorder, cardX, currentY, cardWidth, 22);
                            g.DrawString(text, fontTh, brushBlack, cardX + 8, currentY + 3);

                            cardX += cardWidth + 10;
                            if (cardX + 150 > left + width) break;
                        }
                        currentY += 32;
                    }
                }

                // Draw Table
                var dt = dataset.Data;
                int cols = dt.Columns.Count;
                if (cols == 0) return;

                int colWidth = width / cols;
                int rowHeight = 22;

                // Table Header
                g.FillRectangle(thBrush, left, currentY, width, rowHeight);
                g.DrawRectangle(penBorder, left, currentY, width, rowHeight);

                for (int c = 0; c < cols; c++)
                {
                    string colName = dt.Columns[c].ColumnName;
                    g.DrawString(colName, fontTh, brushBlack, left + c * colWidth + 4, currentY + 3);
                }
                currentY += rowHeight;

                // Rows
                while (currentRowIndex < dt.Rows.Count)
                {
                    if (currentY + rowHeight > bottom - 30)
                    {
                        pageNumber++;
                        e.HasMorePages = true;
                        return;
                    }

                    var row = dt.Rows[currentRowIndex];
                    if (currentRowIndex % 2 == 1)
                    {
                        using var altBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
                        g.FillRectangle(altBrush, left, currentY, width, rowHeight);
                    }
                    g.DrawRectangle(penBorder, left, currentY, width, rowHeight);

                    for (int c = 0; c < cols; c++)
                    {
                        object val = row[c];
                        string text;
                        if (val is decimal dec) text = dec.ToString("N2");
                        else text = val?.ToString() ?? "";

                        g.DrawString(text, fontRow, brushBlack, left + c * colWidth + 4, currentY + 3);
                    }

                    currentY += rowHeight;
                    currentRowIndex++;
                }

                // Page Footer
                string pageFooter = $"Page {pageNumber} | Printed on {DateTime.Now:dd/MM/yyyy HH:mm}";
                g.DrawString(pageFooter, fontSmall, brushGray, left, bottom - 15);

                e.HasMorePages = false;
                currentRowIndex = 0;
                pageNumber = 1;
            };

            if (showPreview)
            {
                using var previewDlg = new PrintPreviewDialog();
                previewDlg.Document = printDoc;
                previewDlg.Width = 950;
                previewDlg.Height = 700;
                previewDlg.StartPosition = FormStartPosition.CenterScreen;
                previewDlg.ShowDialog();
            }
            else
            {
                printDoc.Print();
            }
        }
    }
}
