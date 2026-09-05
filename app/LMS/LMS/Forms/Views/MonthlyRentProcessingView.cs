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
    public class MonthlyRentProcessingView : UserControl
    {
        private ComboBox cmbMonth = null!;
        private NumericUpDown numYear = null!;
        private ComboBox cmbProperty = null!;
        private ComboBox cmbStatus = null!;
        private ModernButton btnBatchGenerate = null!;
        private ModernButton btnPay = null!;
        private ModernDataGridView dgvSchedules = null!;

        private Label lblExpected = null!;
        private Label lblReceived = null!;
        private Label lblPending = null!;
        private Label lblOverdue = null!;

        private List<RentSchedule> _schedules = new();

        public MonthlyRentProcessingView()
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
                "💵 Monthly Rent Billing & Demand Processing",
                "Generate monthly rent schedules, track payment progress per tenant, and adjust utility or maintenance dues."
            );
            Controls.Add(pnlHeader);

            // 2. Filter & Action Bar
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
            cmbMonth.SelectedIndexChanged += (s, e) => LoadSchedules();
            topFlow.Controls.Add(cmbMonth);

            topFlow.Controls.Add(new Label { Text = "Year:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            numYear = new NumericUpDown { Size = new Size(70, 26), Font = new Font("Segoe UI", 9.5f), Minimum = 2020, Maximum = 2050, Value = DateTime.Now.Year, Margin = new Padding(0, 3, 10, 0) };
            numYear.ValueChanged += (s, e) => LoadSchedules();
            topFlow.Controls.Add(numYear);

            topFlow.Controls.Add(new Label { Text = "Property:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            cmbProperty = new ComboBox { Size = new Size(150, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbProperty.SelectedIndexChanged += (s, e) => LoadSchedules();
            topFlow.Controls.Add(cmbProperty);

            topFlow.Controls.Add(new Label { Text = "Status:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary });
            cmbStatus = new ComboBox { Size = new Size(105, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 10, 0) };
            cmbStatus.Items.AddRange(new object[] { "All", "Pending", "Partial", "Paid", "Overdue" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.SelectedIndexChanged += (s, e) => LoadSchedules();
            topFlow.Controls.Add(cmbStatus);

            btnBatchGenerate = new ModernButton { Text = "⚡ Batch Generate", StyleType = ButtonStyleType.Primary, Size = new Size(140, 34), Margin = new Padding(0, 0, 8, 0) };
            btnBatchGenerate.Click += BtnBatchGenerate_Click;
            topFlow.Controls.Add(btnBatchGenerate);

            btnPay = new ModernButton { Text = "💳 Record Payment", StyleType = ButtonStyleType.Success, Size = new Size(145, 34), Margin = new Padding(0, 0, 0, 0) };
            btnPay.Click += BtnPay_Click;
            topFlow.Controls.Add(btnPay);

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

            lblExpected = new Label { Text = "Expected: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblReceived = new Label { Text = "Received: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.SuccessText, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblPending = new Label { Text = "Pending: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.WarningText, AutoSize = true, Margin = new Padding(0, 8, 25, 0) };
            lblOverdue = new Label { Text = "Overdue: Rs. 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.DangerText, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };

            summaryFlow.Controls.Add(lblExpected);
            summaryFlow.Controls.Add(lblReceived);
            summaryFlow.Controls.Add(lblPending);
            summaryFlow.Controls.Add(lblOverdue);
            pnlSummary.Controls.Add(summaryFlow);
            Controls.Add(pnlSummary);

            // 4. DataGridView Card Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            dgvSchedules = new ModernDataGridView { Dock = DockStyle.Fill };
            dgvSchedules.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnPay_Click(this, EventArgs.Empty); };
            pnlGridCard.Controls.Add(dgvSchedules);
            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += MonthlyRentProcessingView_Load;
        }

        private void MonthlyRentProcessingView_Load(object? sender, EventArgs e)
        {
            var props = PropertyService.GetAllProperties();
            var propList = new List<object> { new { Id = 0, Name = "All Properties" } };
            foreach (var p in props) propList.Add(new { Id = p.Id, Name = p.Name });

            cmbProperty.DisplayMember = "Name";
            cmbProperty.ValueMember = "Id";
            cmbProperty.DataSource = propList;

            LoadSchedules();
        }

        public void LoadSchedules()
        {
            int year = (int)numYear.Value;
            int month = cmbMonth.SelectedIndex + 1;
            int? propId = (cmbProperty.SelectedValue is int pid && pid > 0) ? pid : null;

            RentScheduleStatus? status = null;
            if (cmbStatus.SelectedIndex == 1) status = RentScheduleStatus.Pending;
            else if (cmbStatus.SelectedIndex == 2) status = RentScheduleStatus.Partial;
            else if (cmbStatus.SelectedIndex == 3) status = RentScheduleStatus.Paid;
            else if (cmbStatus.SelectedIndex == 4) status = RentScheduleStatus.Overdue;

            _schedules = BillingService.GetRentSchedules(year, month, propId, status);

            var list = _schedules.Select(s => new
            {
                TenantCode = s.RentAgreement?.Tenant?.TenantCode ?? "",
                TenantName = s.RentAgreement?.Tenant?.FullName ?? "",
                Phone = s.RentAgreement?.Tenant?.ContactNumber ?? "",
                Property = s.RentAgreement?.PropertyUnit?.Property?.Name ?? "",
                Unit = s.RentAgreement?.PropertyUnit?.UnitNumber ?? "",
                BaseRent = s.BaseRent.ToString("N0"),
                OtherCharges = (s.UtilityCharges + s.MaintenanceCharges + s.LateFee).ToString("N0"),
                TotalDue = s.TotalDue.ToString("N0"),
                AmountPaid = s.AmountPaid.ToString("N0"),
                Balance = s.Balance.ToString("N0"),
                DueDate = s.DueDate.ToString("dd/MM/yyyy"),
                Status = s.Status.ToString(),
                Id = s.Id,
                TenantId = s.RentAgreement?.TenantId
            }).ToList();

            dgvSchedules.DataSource = list;
            if (dgvSchedules.Columns.Contains("Id")) dgvSchedules.Columns["Id"].Visible = false;
            if (dgvSchedules.Columns.Contains("TenantId")) dgvSchedules.Columns["TenantId"].Visible = false;

            // Summary metrics
            var sum = BillingService.GetMonthlyRentSummary(year, month);
            lblExpected.Text = $"Expected: {SettingService.FormatCurrency(sum.Expected)}";
            lblReceived.Text = $"Received: {SettingService.FormatCurrency(sum.Received)}";
            lblPending.Text = $"Pending: {SettingService.FormatCurrency(sum.Pending)}";
            lblOverdue.Text = $"Overdue: {SettingService.FormatCurrency(sum.Overdue)}";
        }

        private void BtnBatchGenerate_Click(object? sender, EventArgs e)
        {
            int year = (int)numYear.Value;
            int month = cmbMonth.SelectedIndex + 1;

            var res = BillingService.GenerateMonthlyRent(year, month);
            ModernMessageBox.ShowInfo($"Batch Rent Generation for {new DateTime(year, month, 1):MMMM yyyy}:\n• Generated: {res.GeneratedCount} schedules\n• Already Existing: {res.AlreadyExistingCount} schedules\n• Total Demanded: {SettingService.FormatCurrency(res.TotalDemanded)}", "Rent Generated", this);
            LoadSchedules();
        }

        private void BtnPay_Click(object? sender, EventArgs e)
        {
            if (dgvSchedules.CurrentRow == null || !dgvSchedules.Columns.Contains("TenantId"))
            {
                ModernMessageBox.ShowInfo("Please select a tenant schedule in the table.", "Selection Required", this);
                return;
            }

            int tenantId = Convert.ToInt32(dgvSchedules.CurrentRow.Cells["TenantId"].Value);
            string period = $"{cmbMonth.SelectedItem} {numYear.Value}";

            using var dlg = new RecordPaymentForm(tenantId, null, period);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadSchedules();
            }
        }
    }
}
