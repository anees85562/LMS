using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class LedgerView : UserControl
    {
        private ComboBox cmbTenant = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private CheckBox chkAllDates = null!;
        private ModernButton btnRefresh = null!;
        private ModernButton btnPrint = null!;
        private ModernButton btnExportCsv = null!;
        private ModernButton btnExportExcel = null!;
        private ModernButton btnVoid = null!;
        private ModernDataGridView dgvLedger = null!;

        private Label lblOpeningBal = null!;
        private Label lblTotalDebit = null!;
        private Label lblTotalCredit = null!;
        private Label lblClosingBal = null!;

        private List<Tenant> _tenants = new();
        private TenantLedgerStatement? _currentStatement;

        public LedgerView(int? initialTenantId = null)
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
                "📜 Customer / Tenant Digital Ledger & Statement of Account",
                "Complete audit-proof financial ledger with double-entry debit charges, credit collections, and real-time running balance tracking."
            );
            Controls.Add(pnlHeader);

            // 2. Filter Bar
            var filterBar = UIHelper.CreateFilterBar(54);
            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            topFlow.Controls.Add(new Label { Text = "Party:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            cmbTenant = new ComboBox { Size = new Size(200, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbTenant.SelectedIndexChanged += (s, e) => LoadLedger();
            topFlow.Controls.Add(cmbTenant);

            topFlow.Controls.Add(new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            dtpFrom = new DateTimePicker { Size = new Size(110, 26), Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-6), Margin = new Padding(0, 3, 8, 0) };
            topFlow.Controls.Add(dtpFrom);

            topFlow.Controls.Add(new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            dtpTo = new DateTimePicker { Size = new Size(110, 26), Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Now, Margin = new Padding(0, 3, 8, 0) };
            topFlow.Controls.Add(dtpTo);

            chkAllDates = new CheckBox { Text = "All Dates", AutoSize = true, Checked = true, Font = new Font("Segoe UI", 9f), Margin = new Padding(0, 6, 10, 0) };
            chkAllDates.CheckedChanged += (s, e) =>
            {
                dtpFrom.Enabled = !chkAllDates.Checked;
                dtpTo.Enabled = !chkAllDates.Checked;
                LoadLedger();
            };
            topFlow.Controls.Add(chkAllDates);

            btnRefresh = new ModernButton { Text = "Load", StyleType = ButtonStyleType.Primary, Size = new Size(70, 34), Margin = new Padding(0, 0, 6, 0) };
            btnRefresh.Click += (s, e) => LoadLedger();
            topFlow.Controls.Add(btnRefresh);

            btnPrint = new ModernButton { Text = "🖨️ Print", StyleType = ButtonStyleType.Secondary, Size = new Size(85, 34), Margin = new Padding(0, 0, 6, 0) };
            btnPrint.Click += BtnPrint_Click;
            topFlow.Controls.Add(btnPrint);

            btnExportExcel = new ModernButton { Text = "📊 Excel", StyleType = ButtonStyleType.Secondary, Size = new Size(85, 34), Margin = new Padding(0, 0, 6, 0) };
            btnExportExcel.Click += BtnExportExcel_Click;
            topFlow.Controls.Add(btnExportExcel);

            btnExportCsv = new ModernButton { Text = "📄 CSV", StyleType = ButtonStyleType.Secondary, Size = new Size(80, 34), Margin = new Padding(0, 0, 6, 0) };
            btnExportCsv.Click += BtnExportCsv_Click;
            topFlow.Controls.Add(btnExportCsv);

            btnVoid = new ModernButton { Text = "⚠️ Void Entry", StyleType = ButtonStyleType.Danger, Size = new Size(110, 34), Margin = new Padding(0, 0, 0, 0) };
            btnVoid.Click += BtnVoid_Click;
            topFlow.Controls.Add(btnVoid);

            filterBar.Controls.Add(topFlow);
            Controls.Add(filterBar);

            // 3. Summary Bar
            var pnlSummary = UIHelper.CreateCardPanel(new Padding(16, 10, 16, 10));
            pnlSummary.Dock = DockStyle.Bottom;
            pnlSummary.Height = 56;
            pnlSummary.Margin = new Padding(0, 8, 0, 0);

            var summaryFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            lblOpeningBal = new Label { Text = "Opening: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextSecondary, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblTotalDebit = new Label { Text = "Debits: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblTotalCredit = new Label { Text = "Credits: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.SuccessText, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblClosingBal = new Label { Text = "Closing: Rs. 0", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ThemeColors.DangerText, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };

            summaryFlow.Controls.Add(lblOpeningBal);
            summaryFlow.Controls.Add(lblTotalDebit);
            summaryFlow.Controls.Add(lblTotalCredit);
            summaryFlow.Controls.Add(lblClosingBal);
            pnlSummary.Controls.Add(summaryFlow);
            Controls.Add(pnlSummary);

            // 4. Grid Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            dgvLedger = new ModernDataGridView { Dock = DockStyle.Fill };
            pnlGridCard.Controls.Add(dgvLedger);
            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += LedgerView_Load;
        }

        private void LedgerView_Load(object? sender, EventArgs e)
        {
            _tenants = TenantService.GetAllTenants();
            cmbTenant.DisplayMember = "FullName";
            cmbTenant.ValueMember = "Id";
            cmbTenant.DataSource = _tenants;

            dtpFrom.Enabled = false;
            dtpTo.Enabled = false;

            LoadLedger();
        }

        public void LoadLedger()
        {
            if (cmbTenant.SelectedValue is not int tenantId || tenantId <= 0) return;

            DateTime? from = chkAllDates.Checked ? null : dtpFrom.Value.Date;
            DateTime? to = chkAllDates.Checked ? null : dtpTo.Value.Date;

            _currentStatement = LedgerService.GetTenantLedger(tenantId, from, to);

            var list = _currentStatement.Entries.Select(e => new
            {
                Date = e.Date.ToString("dd/MM/yyyy"),
                Code = e.TransactionCode,
                Type = e.TypeName,
                Description = e.Description,
                Reference = e.Reference ?? "-",
                Method = e.PaymentMethod,
                Debit = e.Debit > 0 ? e.Debit.ToString("N0") : "-",
                Credit = e.Credit > 0 ? e.Credit.ToString("N0") : "-",
                Balance = e.Balance.ToString("N0"),
                Remarks = e.Remarks ?? "-",
                TransactionId = e.TransactionId
            }).ToList();

            dgvLedger.DataSource = list;
            if (dgvLedger.Columns.Contains("TransactionId")) dgvLedger.Columns["TransactionId"].Visible = false;

            if (dgvLedger.Columns.Contains("Description"))
            {
                dgvLedger.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            // Summary
            lblOpeningBal.Text = $"Opening: {SettingService.FormatCurrency(_currentStatement.OpeningBalance)}";
            lblTotalDebit.Text = $"Debits (Charges): {SettingService.FormatCurrency(_currentStatement.TotalDebit)}";
            lblTotalCredit.Text = $"Credits (Paid): {SettingService.FormatCurrency(_currentStatement.TotalCredit)}";
            lblClosingBal.Text = $"Closing Balance: {SettingService.FormatCurrency(_currentStatement.ClosingBalance)}";
            lblClosingBal.ForeColor = _currentStatement.ClosingBalance > 0 ? ThemeColors.Danger : ThemeColors.SuccessText;
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (cmbTenant.SelectedValue is not int tenantId || tenantId <= 0) return;
            DateTime? from = chkAllDates.Checked ? null : dtpFrom.Value.Date;
            DateTime? to = chkAllDates.Checked ? null : dtpTo.Value.Date;

            var ds = ReportService.GetTenantStatementReport(tenantId, from, to);
            PrintingService.PrintReport(ds, showPreview: true);
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            if (_currentStatement == null || _currentStatement.Tenant == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel HTML (*.html)|*.html|CSV (*.csv)|*.csv",
                FileName = $"Statement_{_currentStatement.Tenant.FullName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.html"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DateTime? from = chkAllDates.Checked ? null : dtpFrom.Value.Date;
                DateTime? to = chkAllDates.Checked ? null : dtpTo.Value.Date;
                var ds = ReportService.GetTenantStatementReport(_currentStatement.Tenant.Id, from, to);

                if (sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ImportExportService.ExportToCsv(ds.Data, sfd.FileName);
                }
                else
                {
                    ImportExportService.ExportToHtmlExcel(ds.Data, ds.Title, sfd.FileName);
                }

                ModernMessageBox.ShowInfo($"Ledger statement exported successfully to:\n{sfd.FileName}", "Exported", this);
            }
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_currentStatement == null || _currentStatement.Tenant == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"Statement_{_currentStatement.Tenant.FullName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DateTime? from = chkAllDates.Checked ? null : dtpFrom.Value.Date;
                DateTime? to = chkAllDates.Checked ? null : dtpTo.Value.Date;
                var ds = ReportService.GetTenantStatementReport(_currentStatement.Tenant.Id, from, to);
                ImportExportService.ExportToCsv(ds.Data, sfd.FileName);
                ModernMessageBox.ShowInfo($"Ledger statement exported successfully to:\n{sfd.FileName}", "Exported", this);
            }
        }

        private void BtnVoid_Click(object? sender, EventArgs e)
        {
            if (dgvLedger.CurrentRow == null || !dgvLedger.Columns.Contains("TransactionId"))
            {
                ModernMessageBox.ShowInfo("Please select a transaction row from the ledger to void.", "Selection Required", this);
                return;
            }

            int txId = Convert.ToInt32(dgvLedger.CurrentRow.Cells["TransactionId"].Value);
            string desc = dgvLedger.CurrentRow.Cells["Description"].Value?.ToString() ?? "Transaction";
            string code = dgvLedger.CurrentRow.Cells["Code"].Value?.ToString() ?? "";

            using var dlg = new VoidTransactionForm(txId, $"{code} - {desc}");
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadLedger();
            }
        }
    }
}
