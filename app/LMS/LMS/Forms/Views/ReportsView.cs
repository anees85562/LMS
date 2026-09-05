using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class ReportsView : UserControl
    {
        private ComboBox cmbReportType = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private ComboBox cmbTenantFilter = null!;
        private ComboBox cmbMonth = null!;
        private NumericUpDown numYear = null!;
        private ModernButton btnGenerate = null!;
        private ModernButton btnPrint = null!;
        private ModernButton btnExportExcel = null!;
        private ModernButton btnExportCsv = null!;

        private FlowLayoutPanel pnlKpiCards = null!;
        private ModernDataGridView dgvReport = null!;

        private ReportDataset? _currentReport;
        private List<Tenant> _tenants = new();

        public ReportsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                "📈 Universal Reports, Receivables & Analytics",
                "Generate, preview, print, and export operational, installment, sales, inventory valuation, and financial reports."
            );
            Controls.Add(pnlHeader);

            // 2. Controls Filter Box
            var filterCard = UIHelper.CreateCardPanel(new Padding(12, 10, 12, 10));
            filterCard.Dock = DockStyle.Top;
            filterCard.Height = 90;

            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            topFlow.Controls.Add(new Label { Text = "Select Report:", AutoSize = true, Margin = new Padding(0, 7, 6, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });

            cmbReportType = new ComboBox { Size = new Size(240, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbReportType.Items.AddRange(new object[]
            {
                "1. Sales & Installment Plans Report",
                "2. Universal Defaulters & Aging Report",
                "3. Customer / Tenant Ledger Statement",
                "4. Inventory & Stock Valuation Report",
                "5. Monthly Rent Collection Sheet",
                "6. Property Occupancy & Vacancy",
                "7. Financial Revenue & Expenses",
                "8. System Audit Trail Log"
            });
            cmbReportType.SelectedIndex = 0;
            cmbReportType.SelectedIndexChanged += CmbReportType_SelectedIndexChanged;
            topFlow.Controls.Add(cmbReportType);

            // Month / Year controls (for monthly sheet)
            cmbMonth = new ComboBox { Size = new Size(110, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false, Margin = new Padding(0, 3, 6, 0) };
            cmbMonth.Items.AddRange(new object[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
            cmbMonth.SelectedIndex = DateTime.Now.Month - 1;
            topFlow.Controls.Add(cmbMonth);

            numYear = new NumericUpDown { Size = new Size(70, 26), Font = new Font("Segoe UI", 9.5f), Minimum = 2020, Maximum = 2050, Value = DateTime.Now.Year, Visible = false, Margin = new Padding(0, 3, 10, 0) };
            topFlow.Controls.Add(numYear);

            // Tenant Filter (for Statement)
            cmbTenantFilter = new ComboBox { Size = new Size(200, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Visible = false, Margin = new Padding(0, 3, 10, 0) };
            topFlow.Controls.Add(cmbTenantFilter);

            // Date Range (From / To)
            dtpFrom = new DateTimePicker { Size = new Size(105, 26), Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1), Visible = true, Margin = new Padding(0, 3, 6, 0) };
            dtpTo = new DateTimePicker { Size = new Size(105, 26), Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Now, Visible = true, Margin = new Padding(0, 3, 10, 0) };
            topFlow.Controls.Add(dtpFrom);
            topFlow.Controls.Add(dtpTo);

            // Action Buttons
            btnGenerate = new ModernButton { Text = "⚡ Generate", StyleType = ButtonStyleType.Primary, Size = new Size(105, 34), Margin = new Padding(0, 0, 6, 0) };
            btnGenerate.Click += (s, e) => GenerateReport();
            topFlow.Controls.Add(btnGenerate);

            btnPrint = new ModernButton { Text = "🖨️ Print", StyleType = ButtonStyleType.Secondary, Size = new Size(90, 34), Margin = new Padding(0, 0, 6, 0) };
            btnPrint.Click += BtnPrint_Click;
            topFlow.Controls.Add(btnPrint);

            btnExportExcel = new ModernButton { Text = "📊 Excel", StyleType = ButtonStyleType.Secondary, Size = new Size(90, 34), Margin = new Padding(0, 0, 6, 0) };
            btnExportExcel.Click += BtnExportExcel_Click;
            topFlow.Controls.Add(btnExportExcel);

            btnExportCsv = new ModernButton { Text = "📄 CSV", StyleType = ButtonStyleType.Secondary, Size = new Size(85, 34), Margin = new Padding(0, 0, 0, 0) };
            btnExportCsv.Click += BtnExportCsv_Click;
            topFlow.Controls.Add(btnExportCsv);

            filterCard.Controls.Add(topFlow);

            // Row 2: KPI Summary Strip
            pnlKpiCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            filterCard.Controls.Add(pnlKpiCards);

            Controls.Add(filterCard);

            // 3. Grid Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            dgvReport = new ModernDataGridView { Dock = DockStyle.Fill };
            pnlGridCard.Controls.Add(dgvReport);
            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += ReportsView_Load;
        }

        private void ReportsView_Load(object? sender, EventArgs e)
        {
            _tenants = TenantService.GetAllTenants();
            cmbTenantFilter.DisplayMember = "FullName";
            cmbTenantFilter.ValueMember = "Id";
            cmbTenantFilter.DataSource = _tenants;

            GenerateReport();
        }

        private void CmbReportType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int idx = cmbReportType.SelectedIndex;
            cmbMonth.Visible = (idx == 4);
            numYear.Visible = (idx == 4);
            cmbTenantFilter.Visible = (idx == 2);
            dtpFrom.Visible = (idx == 0 || idx == 2 || idx == 6 || idx == 7);
            dtpTo.Visible = (idx == 0 || idx == 2 || idx == 6 || idx == 7);

            GenerateReport();
        }

        public void GenerateReport()
        {
            int idx = cmbReportType.SelectedIndex;
            switch (idx)
            {
                case 0: // Sales & Installments
                    _currentReport = ReportService.GetInstallmentSalesReport(dtpFrom.Value.Date, dtpTo.Value.Date);
                    break;

                case 1: // Defaulters & Aging
                    _currentReport = ReportService.GetDefaultersReport();
                    break;

                case 2: // Customer Statement
                    int tId = (cmbTenantFilter.SelectedValue is int tid && tid > 0) ? tid : (_tenants.FirstOrDefault()?.Id ?? 0);
                    if (tId > 0)
                    {
                        _currentReport = ReportService.GetTenantStatementReport(tId, dtpFrom.Value.Date, dtpTo.Value.Date);
                    }
                    break;

                case 3: // Stock Inventory
                    _currentReport = ReportService.GetStockInventoryReport();
                    break;

                case 4: // Monthly Rent Sheet
                    int yr = (int)numYear.Value;
                    int mo = cmbMonth.SelectedIndex + 1;
                    _currentReport = ReportService.GetMonthlyRentReport(yr, mo);
                    break;

                case 5: // Vacancy / Occupancy
                    _currentReport = ReportService.GetVacancyReport();
                    break;

                case 6: // Financial Summary
                    _currentReport = ReportService.GetFinancialSummaryReport(dtpFrom.Value.Date, dtpTo.Value.Date);
                    break;

                case 7: // Audit Trail
                    var logs = AuditService.GetLogs(dtpFrom.Value.Date, dtpTo.Value.Date);
                    var dt = new DataTable();
                    dt.Columns.Add("Timestamp", typeof(string));
                    dt.Columns.Add("User", typeof(string));
                    dt.Columns.Add("Action", typeof(string));
                    dt.Columns.Add("Entity", typeof(string));
                    dt.Columns.Add("Entity ID", typeof(string));
                    dt.Columns.Add("Details", typeof(string));

                    foreach (var l in logs)
                    {
                        dt.Rows.Add(l.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"), l.Username, l.Action, l.EntityName, l.EntityId ?? "-", l.Details ?? "-");
                    }

                    _currentReport = new ReportDataset
                    {
                        Title = "System Activity & Audit Log Report",
                        Subtitle = $"From {dtpFrom.Value:dd/MM/yyyy} to {dtpTo.Value:dd/MM/yyyy}",
                        Data = dt
                    };
                    _currentReport.SummaryCards["Total Log Entries"] = logs.Count.ToString();
                    break;
            }

            if (_currentReport != null)
            {
                dgvReport.DataSource = _currentReport.Data;

                // Build Summary Cards Strip
                pnlKpiCards.Controls.Clear();
                foreach (var kvp in _currentReport.SummaryCards)
                {
                    var lbl = new Label
                    {
                        Text = $"• {kvp.Key}: {kvp.Value}",
                        Font = ThemeColors.SmallBoldFont,
                        ForeColor = ThemeColors.PrimaryDark,
                        AutoSize = true,
                        Margin = new Padding(0, 4, 18, 0)
                    };
                    pnlKpiCards.Controls.Add(lbl);
                }
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (_currentReport == null) return;
            PrintingService.PrintReport(_currentReport, showPreview: true);
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            if (_currentReport == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Table (*.html)|*.html|Excel (*.xls)|*.xls",
                FileName = $"{_currentReport.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.html"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ImportExportService.ExportToHtmlExcel(_currentReport.Data, _currentReport.Title, sfd.FileName);
                ModernMessageBox.ShowInfo($"Report exported successfully to:\n{sfd.FileName}", "Exported", this);
            }
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_currentReport == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"{_currentReport.Title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                ImportExportService.ExportToCsv(_currentReport.Data, sfd.FileName);
                ModernMessageBox.ShowInfo($"Report exported successfully to:\n{sfd.FileName}", "Exported", this);
            }
        }
    }
}
