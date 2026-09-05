using System;
using System.Collections.Generic;
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
    public class RegisterView : UserControl
    {
        private ComboBox cmbMonth = null!;
        private NumericUpDown numYear = null!;
        private ComboBox cmbProperty = null!;
        private TextBox txtSearch = null!;
        private ModernButton btnGenerate = null!;
        private ModernButton btnPrint = null!;
        private ModernButton btnRecordPayment = null!;
        private ModernDataGridView dgvRegister = null!;

        private Label lblSummaryDemanded = null!;
        private Label lblSummaryPaid = null!;
        private Label lblSummaryBalance = null!;
        private Label lblSummaryRate = null!;

        private List<TraditionalRegisterRow> _allRows = new();

        public RegisterView()
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
                "📖 Universal Receivables & Digital Rent Register",
                "Digital matrix register tracking monthly demands, previous arrears, current recoveries, and net balances per customer/unit."
            );
            Controls.Add(pnlHeader);

            // 2. Filter Bar Box
            var filterBar = UIHelper.CreateFilterBar(54);
            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            topFlow.Controls.Add(new Label { Text = "Month:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            cmbMonth = new ComboBox { Size = new Size(115, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbMonth.Items.AddRange(new object[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
            cmbMonth.SelectedIndex = DateTime.Now.Month - 1;
            cmbMonth.SelectedIndexChanged += (s, e) => LoadRegisterData();
            topFlow.Controls.Add(cmbMonth);

            topFlow.Controls.Add(new Label { Text = "Year:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            numYear = new NumericUpDown { Size = new Size(70, 26), Font = new Font("Segoe UI", 9.5f), Minimum = 2020, Maximum = 2050, Value = DateTime.Now.Year, Margin = new Padding(0, 3, 10, 0) };
            numYear.ValueChanged += (s, e) => LoadRegisterData();
            topFlow.Controls.Add(numYear);

            topFlow.Controls.Add(new Label { Text = "Property:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            cmbProperty = new ComboBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbProperty.SelectedIndexChanged += (s, e) => LoadRegisterData();
            topFlow.Controls.Add(cmbProperty);

            txtSearch = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "Search tenant/unit...", Margin = new Padding(0, 3, 10, 0) };
            txtSearch.TextChanged += (s, e) => FilterRegisterData();
            topFlow.Controls.Add(txtSearch);

            btnGenerate = new ModernButton
            {
                Text = "⚡ Batch Generate",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(140, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            btnGenerate.Click += BtnGenerate_Click;
            topFlow.Controls.Add(btnGenerate);

            btnPrint = new ModernButton
            {
                Text = "🖨️ Print Register",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(130, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            btnPrint.Click += BtnPrint_Click;
            topFlow.Controls.Add(btnPrint);

            filterBar.Controls.Add(topFlow);
            Controls.Add(filterBar);

            // 3. Bottom Summary Bar
            var pnlBottom = UIHelper.CreateCardPanel(new Padding(16, 10, 16, 10));
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 56;
            pnlBottom.Margin = new Padding(0, 8, 0, 0);

            var bottomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            lblSummaryDemanded = new Label { Text = "Demanded: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, AutoSize = true, Margin = new Padding(0, 8, 20, 0) };
            lblSummaryPaid = new Label { Text = "Paid: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.SuccessText, AutoSize = true, Margin = new Padding(0, 8, 20, 0) };
            lblSummaryBalance = new Label { Text = "Balance: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.DangerText, AutoSize = true, Margin = new Padding(0, 8, 20, 0) };
            lblSummaryRate = new Label { Text = "Collection: 0%", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.Primary, AutoSize = true, Margin = new Padding(0, 8, 20, 0) };

            btnRecordPayment = new ModernButton
            {
                Text = "💳 Pay Selected",
                StyleType = ButtonStyleType.Success,
                Size = new Size(130, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            btnRecordPayment.Click += BtnRecordPayment_Click;

            bottomFlow.Controls.Add(lblSummaryDemanded);
            bottomFlow.Controls.Add(lblSummaryPaid);
            bottomFlow.Controls.Add(lblSummaryBalance);
            bottomFlow.Controls.Add(lblSummaryRate);
            bottomFlow.Controls.Add(btnRecordPayment);

            pnlBottom.Controls.Add(bottomFlow);
            Controls.Add(pnlBottom);

            // 4. Grid Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            dgvRegister = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            dgvRegister.CellDoubleClick += DgvRegister_CellDoubleClick;
            pnlGridCard.Controls.Add(dgvRegister);
            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += RegisterView_Load;
        }

        private void RegisterView_Load(object? sender, EventArgs e)
        {
            var props = PropertyService.GetAllProperties();
            var propList = new List<object> { new { Id = 0, Name = "All Properties" } };
            foreach (var p in props)
            {
                propList.Add(new { Id = p.Id, Name = p.Name });
            }

            cmbProperty.DisplayMember = "Name";
            cmbProperty.ValueMember = "Id";
            cmbProperty.DataSource = propList;

            LoadRegisterData();
        }

        public void LoadRegisterData()
        {
            int year = (int)numYear.Value;
            int month = cmbMonth.SelectedIndex + 1;
            int? propId = null;

            if (cmbProperty.SelectedValue is int pid && pid > 0)
            {
                propId = pid;
            }

            _allRows = LedgerService.GetTraditionalRegisterMatrix(year, month, propId);
            FilterRegisterData();
        }

        private void FilterRegisterData()
        {
            string search = txtSearch.Text.Trim().ToLower();
            var filtered = _allRows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(r =>
                    r.TenantName.ToLower().Contains(search) ||
                    r.TenantCode.ToLower().Contains(search) ||
                    r.UnitNumber.ToLower().Contains(search) ||
                    r.PropertyName.ToLower().Contains(search) ||
                    r.Phone.Contains(search)
                );
            }

            var list = filtered.Select(r => new
            {
                TenantCode = r.TenantCode,
                TenantName = r.TenantName,
                Phone = r.Phone,
                Property = r.PropertyName,
                Unit = r.UnitNumber,
                RentDemanded = r.CurrentRentDemanded.ToString("N0"),
                Arrears = r.PreviousArrears.ToString("N0"),
                TotalDemanded = r.TotalDemanded.ToString("N0"),
                PaidThisMonth = r.PaidThisMonth.ToString("N0"),
                NetBalance = r.NetBalance.ToString("N0"),
                Status = r.Status,
                LastPayment = r.LastPaymentDate.HasValue ? r.LastPaymentDate.Value.ToString("dd/MM/yyyy") : "-",
                ReceiptNo = r.LastReceiptNumber ?? "-",
                TenantId = r.TenantId,
                AgreementId = r.AgreementId
            }).ToList();

            dgvRegister.DataSource = list;

            if (dgvRegister.Columns.Contains("TenantId")) dgvRegister.Columns["TenantId"].Visible = false;
            if (dgvRegister.Columns.Contains("AgreementId")) dgvRegister.Columns["AgreementId"].Visible = false;

            if (dgvRegister.Columns.Contains("TenantName"))
            {
                dgvRegister.Columns["TenantName"].HeaderText = "Customer / Tenant";
                dgvRegister.Columns["TenantCode"].HeaderText = "Code";
                dgvRegister.Columns["RentDemanded"].HeaderText = "Current Rent";
                dgvRegister.Columns["TotalDemanded"].HeaderText = "Total Demanded";
                dgvRegister.Columns["PaidThisMonth"].HeaderText = "Paid";
                dgvRegister.Columns["NetBalance"].HeaderText = "Balance";
                dgvRegister.Columns["LastPayment"].HeaderText = "Pay Date";
                dgvRegister.Columns["ReceiptNo"].HeaderText = "Receipt #";
            }

            // Update Summary Bar
            decimal totalDem = _allRows.Sum(r => r.TotalDemanded);
            decimal totalPaid = _allRows.Sum(r => r.PaidThisMonth);
            decimal totalBal = _allRows.Sum(r => r.NetBalance);
            double rate = totalDem > 0 ? (double)(totalPaid / totalDem * 100) : 0;

            lblSummaryDemanded.Text = $"Total Demanded: {SettingService.FormatCurrency(totalDem)}";
            lblSummaryPaid.Text = $"Total Paid: {SettingService.FormatCurrency(totalPaid)}";
            lblSummaryBalance.Text = $"Net Pending Balance: {SettingService.FormatCurrency(totalBal)}";
            lblSummaryRate.Text = $"Collection: {rate:N1}% ({_allRows.Count} units)";
        }

        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            int year = (int)numYear.Value;
            int month = cmbMonth.SelectedIndex + 1;

            var res = BillingService.GenerateMonthlyRent(year, month);
            ModernMessageBox.ShowInfo($"Rent generation completed for {new DateTime(year, month, 1):MMMM yyyy}:\n• Generated: {res.GeneratedCount} units\n• Already Existing: {res.AlreadyExistingCount} units\n• Total Demanded: {SettingService.FormatCurrency(res.TotalDemanded)}", "Rent Generated", this);
            LoadRegisterData();
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            int year = (int)numYear.Value;
            int month = cmbMonth.SelectedIndex + 1;
            int? propId = (cmbProperty.SelectedValue is int pid && pid > 0) ? pid : null;

            var ds = ReportService.GetMonthlyRentReport(year, month, propId);
            PrintingService.PrintReport(ds, showPreview: true);
        }

        private void BtnRecordPayment_Click(object? sender, EventArgs e)
        {
            if (dgvRegister.CurrentRow == null || !dgvRegister.Columns.Contains("TenantId"))
            {
                ModernMessageBox.ShowInfo("Please select a tenant row in the register table.", "Selection Required", this);
                return;
            }

            int tenantId = Convert.ToInt32(dgvRegister.CurrentRow.Cells["TenantId"].Value);
            string monthYearStr = $"{cmbMonth.SelectedItem} {numYear.Value}";

            using var dlg = new RecordPaymentForm(tenantId, null, monthYearStr);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadRegisterData();
            }
        }

        private void DgvRegister_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                BtnRecordPayment_Click(this, EventArgs.Empty);
            }
        }
    }
}
