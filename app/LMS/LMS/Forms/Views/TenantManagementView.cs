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
    public class TenantManagementView : UserControl
    {
        private TextBox txtSearch = null!;
        private ComboBox cmbStatusFilter = null!;
        private ComboBox cmbTypeFilter = null!;
        private ModernButton btnAddTenant = null!;
        private ModernButton btnEditTenant = null!;
        private ModernButton btnArchiveTenant = null!;
        private ModernDataGridView dgvTenants = null!;

        // Right Detail Card Controls
        private Label lblTenantName = null!;
        private Label lblTenantMeta = null!;
        private Label lblContact = null!;
        private Label lblCnic = null!;
        private Label lblAddress = null!;
        private Label lblGuarantor = null!;

        private Panel pnlLeaseBox = null!;
        private Label lblLeaseProp = null!;
        private Label lblLeaseRent = null!;
        private Label lblLeasePeriod = null!;

        private Label lblBalance = null!;
        private ModernButton btnPay = null!;
        private ModernButton btnNewLease = null!;
        private ModernButton btnPrintStatement = null!;

        private List<Tenant> _allTenants = new();
        private Tenant? _selectedTenant;
        private Action<string, int?>? _navigationCallback;
        private TerminologyService _terminology = new TerminologyService(new SettingService());

        public TenantManagementView(Action<string, int?>? navigationCallback = null)
        {
            _navigationCallback = navigationCallback;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);

            string custPlural = _terminology.CustomerPlural;
            string custSingular = _terminology.CustomerSingular;

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                $"👥 {custPlural} & Party Directory",
                $"Comprehensive directory of {custPlural.ToLower()}, contact numbers, CNIC, credit limits, active agreements, and real-time ledger balances."
            );
            Controls.Add(pnlHeader);

            // 2. Split Container
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 650,
                BackColor = ThemeColors.Border,
                Margin = new Padding(0, 8, 0, 0)
            };

            // LEFT: Customer Table
            var pnlLeft = UIHelper.CreateCardPanel(new Padding(12));
            pnlLeft.Dock = DockStyle.Fill;

            var pnlLeftTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };

            var lblLeftHeader = new Label { Text = $"{custPlural} Directory", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.TextPrimary, Location = new Point(0, 4), AutoSize = true };
            pnlLeftTop.Controls.Add(lblLeftHeader);

            var filterFlow = new FlowLayoutPanel { Location = new Point(0, 36), Size = new Size(620, 38), WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };

            txtSearch = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "Search name/phone...", Margin = new Padding(0, 3, 6, 0) };
            txtSearch.TextChanged += (s, e) => FilterTenants();
            filterFlow.Controls.Add(txtSearch);

            cmbTypeFilter = new ComboBox { Size = new Size(115, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 6, 0) };
            cmbTypeFilter.Items.Add("All Types");
            cmbTypeFilter.Items.Add(CustomerType.Tenant);
            cmbTypeFilter.Items.Add(CustomerType.InstallmentCustomer);
            cmbTypeFilter.Items.Add(CustomerType.BNPLCreditCustomer);
            cmbTypeFilter.Items.Add(CustomerType.GeneralParty);
            cmbTypeFilter.SelectedIndex = 0;
            cmbTypeFilter.SelectedIndexChanged += (s, e) => LoadTenants();
            filterFlow.Controls.Add(cmbTypeFilter);

            cmbStatusFilter = new ComboBox { Size = new Size(95, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 8, 0) };
            cmbStatusFilter.Items.AddRange(new object[] { "Active", "All Status", "Previous" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadTenants();
            filterFlow.Controls.Add(cmbStatusFilter);

            btnAddTenant = new ModernButton { Text = "+ Add", StyleType = ButtonStyleType.Primary, Size = new Size(70, 32), Margin = new Padding(0, 0, 4, 0) };
            btnAddTenant.Click += BtnAddTenant_Click;
            filterFlow.Controls.Add(btnAddTenant);

            btnEditTenant = new ModernButton { Text = "Edit", StyleType = ButtonStyleType.Secondary, Size = new Size(65, 32), Margin = new Padding(0, 0, 4, 0) };
            btnEditTenant.Click += BtnEditTenant_Click;
            filterFlow.Controls.Add(btnEditTenant);

            btnArchiveTenant = new ModernButton { Text = "Archive", StyleType = ButtonStyleType.Danger, Size = new Size(75, 32), Margin = new Padding(0, 0, 0, 0) };
            btnArchiveTenant.Click += BtnArchiveTenant_Click;
            filterFlow.Controls.Add(btnArchiveTenant);

            pnlLeftTop.Controls.Add(filterFlow);
            pnlLeft.Controls.Add(pnlLeftTop);

            dgvTenants = new ModernDataGridView { Dock = DockStyle.Fill };
            dgvTenants.SelectionChanged += DgvTenants_SelectionChanged;
            pnlLeft.Controls.Add(dgvTenants);
            dgvTenants.BringToFront();

            split.Panel1.Controls.Add(pnlLeft);

            // RIGHT: Profile Pane
            var pnlRight = UIHelper.CreateCardPanel(new Padding(16));
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.AutoScroll = true;

            int ry = 10;
            lblTenantName = new Label { Text = $"Select a {custSingular}", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, Location = new Point(12, ry), AutoSize = true };
            pnlRight.Controls.Add(lblTenantName);
            ry += 28;

            lblTenantMeta = new Label { Text = "Code: - | Type: - | Rating: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextSecondary, Location = new Point(14, ry), AutoSize = true };
            pnlRight.Controls.Add(lblTenantMeta);
            ry += 32;

            // Personal Info Card
            var pnlPersonal = new Panel { Location = new Point(12, ry), Size = new Size(390, 115), BackColor = ThemeColors.CanvasBg };
            pnlPersonal.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlPersonal.Width - 1, pnlPersonal.Height - 1);
            };

            lblContact = new Label { Text = "Phone: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextPrimary, Location = new Point(10, 8), AutoSize = true };
            lblCnic = new Label { Text = "CNIC / ID: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextPrimary, Location = new Point(10, 30), AutoSize = true };
            lblAddress = new Label { Text = "Address: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextSecondary, Location = new Point(10, 52), Size = new Size(370, 24) };
            lblGuarantor = new Label { Text = "Guarantor: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextSecondary, Location = new Point(10, 78), Size = new Size(370, 24) };

            pnlPersonal.Controls.Add(lblContact);
            pnlPersonal.Controls.Add(lblCnic);
            pnlPersonal.Controls.Add(lblAddress);
            pnlPersonal.Controls.Add(lblGuarantor);
            pnlRight.Controls.Add(pnlPersonal);
            ry += 125;

            // Active Deal / Lease Card
            pnlLeaseBox = new Panel { Location = new Point(12, ry), Size = new Size(390, 95), BackColor = ThemeColors.PrimaryLight };
            pnlLeaseBox.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(191, 219, 254));
                e.Graphics.DrawRectangle(pen, 0, 0, pnlLeaseBox.Width - 1, pnlLeaseBox.Height - 1);
            };

            var lblLeaseTitle = new Label { Text = "ACTIVE CONTRACT / LEASE", Font = new Font("Segoe UI", 8.2f, FontStyle.Bold), ForeColor = ThemeColors.PrimaryDark, Location = new Point(10, 8), AutoSize = true };
            lblLeaseProp = new Label { Text = "Unit / Account: None", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, Location = new Point(10, 28), AutoSize = true };
            lblLeaseRent = new Label { Text = "Rate / Plan: -", Font = new Font("Segoe UI", 8.8f), ForeColor = ThemeColors.TextPrimary, Location = new Point(10, 50), AutoSize = true };
            lblLeasePeriod = new Label { Text = "Active Deals Count: 0", Font = new Font("Segoe UI", 8.5f), ForeColor = ThemeColors.TextSecondary, Location = new Point(10, 70), AutoSize = true };

            pnlLeaseBox.Controls.Add(lblLeaseTitle);
            pnlLeaseBox.Controls.Add(lblLeaseProp);
            pnlLeaseBox.Controls.Add(lblLeaseRent);
            pnlLeaseBox.Controls.Add(lblLeasePeriod);
            pnlRight.Controls.Add(pnlLeaseBox);
            ry += 105;

            // Balance Card
            var pnlBalBox = new Panel { Location = new Point(12, ry), Size = new Size(390, 65), BackColor = Color.White };
            pnlBalBox.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlBalBox.Width - 1, pnlBalBox.Height - 1);
            };

            var lblBalTitle = new Label { Text = "CURRENT OUTSTANDING BALANCE", Font = new Font("Segoe UI", 8.2f, FontStyle.Bold), ForeColor = ThemeColors.TextMuted, Location = new Point(10, 8), AutoSize = true };
            lblBalance = new Label { Text = "Rs. 0", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = ThemeColors.SuccessText, Location = new Point(10, 26), AutoSize = true };
            pnlBalBox.Controls.Add(lblBalTitle);
            pnlBalBox.Controls.Add(lblBalance);
            pnlRight.Controls.Add(pnlBalBox);
            ry += 76;

            // Action Buttons
            btnPay = new ModernButton { Text = "💳 Record Payment", StyleType = ButtonStyleType.Success, Location = new Point(12, ry), Size = new Size(185, 38) };
            btnPay.Click += BtnPay_Click;
            pnlRight.Controls.Add(btnPay);

            btnNewLease = new ModernButton { Text = "📝 Lease Agreement", StyleType = ButtonStyleType.Primary, Location = new Point(207, ry), Size = new Size(195, 38) };
            btnNewLease.Click += BtnNewLease_Click;
            pnlRight.Controls.Add(btnNewLease);
            ry += 46;

            btnPrintStatement = new ModernButton { Text = "🖨️ Complete Statement of Account", StyleType = ButtonStyleType.Secondary, Location = new Point(12, ry), Size = new Size(390, 36) };
            btnPrintStatement.Click += BtnPrintStatement_Click;
            pnlRight.Controls.Add(btnPrintStatement);

            split.Panel2.Controls.Add(pnlRight);
            Controls.Add(split);

            split.SendToBack();
            pnlHeader.BringToFront();

            Load += (s, e) => LoadTenants();
        }

        public void LoadTenants()
        {
            TenantStatus? statusFilter = null;
            if (cmbStatusFilter.SelectedIndex == 0) statusFilter = TenantStatus.Active;
            else if (cmbStatusFilter.SelectedIndex == 2) statusFilter = TenantStatus.Previous;

            CustomerType? typeFilter = cmbTypeFilter.SelectedItem is CustomerType ct ? ct : null;

            _allTenants = TenantService.GetAllTenants(statusFilter, null, typeFilter);
            FilterTenants();
        }

        private void FilterTenants()
        {
            string s = txtSearch.Text.Trim().ToLower();
            var list = _allTenants.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(s))
            {
                list = list.Where(t =>
                    t.FullName.ToLower().Contains(s) ||
                    t.TenantCode.ToLower().Contains(s) ||
                    t.ContactNumber.Contains(s) ||
                    (t.CnicOrId != null && t.CnicOrId.Contains(s)) ||
                    (t.City != null && t.City.ToLower().Contains(s))
                );
            }

            var table = list.Select(t =>
            {
                decimal bal = TenantService.GetTenantCurrentBalance(t.Id);

                return new
                {
                    Code = t.TenantCode,
                    Name = t.FullName,
                    Type = t.CustomerType.ToString(),
                    Phone = t.ContactNumber,
                    City = t.City ?? "-",
                    CreditLimit = t.CreditLimit > 0 ? SettingService.FormatCurrency(t.CreditLimit) : "No Limit",
                    Balance = SettingService.FormatCurrency(bal),
                    Rating = t.Rating ?? "Good",
                    Status = t.Status.ToString(),
                    Id = t.Id
                };
            }).ToList();

            dgvTenants.DataSource = table;
            if (dgvTenants.Columns.Contains("Id")) dgvTenants.Columns["Id"].Visible = false;
        }

        private void DgvTenants_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvTenants.CurrentRow != null && dgvTenants.CurrentRow.Cells["Id"].Value is int tenantId)
            {
                _selectedTenant = _allTenants.FirstOrDefault(t => t.Id == tenantId);
                DisplayTenantDetails();
            }
            else
            {
                _selectedTenant = null;
                ClearTenantDetails();
            }
        }

        private void DisplayTenantDetails()
        {
            if (_selectedTenant == null) return;

            lblTenantName.Text = _selectedTenant.FullName;
            lblTenantMeta.Text = $"Code: {_selectedTenant.TenantCode} | Type: {_selectedTenant.CustomerType} | Rating: {_selectedTenant.Rating}";
            lblContact.Text = $"Phone: {_selectedTenant.ContactNumber} {(!string.IsNullOrWhiteSpace(_selectedTenant.AlternateContact) ? $"| Alt: {_selectedTenant.AlternateContact}" : "")}";
            lblCnic.Text = $"CNIC / ID: {_selectedTenant.CnicOrId ?? "-"}";
            lblAddress.Text = $"Address: {_selectedTenant.PermanentAddress ?? "-"} | City: {_selectedTenant.City ?? "-"}";
            lblGuarantor.Text = !string.IsNullOrWhiteSpace(_selectedTenant.GuarantorName)
                ? $"Guarantor: {_selectedTenant.GuarantorName} ({_selectedTenant.GuarantorPhone}) [{_selectedTenant.GuarantorRelation}]"
                : "Guarantor: None specified";

            var lease = _selectedTenant.RentAgreements.FirstOrDefault(a => a.Status == AgreementStatus.Active);
            int activeSales = _selectedTenant.InstallmentSales.Count(s => s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid);

            if (lease != null && lease.PropertyUnit != null)
            {
                lblLeaseProp.Text = $"Unit: {lease.PropertyUnit.Property?.Name} - {lease.PropertyUnit.UnitNumber}";
                lblLeaseRent.Text = $"Monthly Rent: {SettingService.FormatCurrency(lease.MonthlyRent)} (Due on {lease.DueDayOfMonth}th)";
                lblLeasePeriod.Text = $"Active Lease: {lease.StartDate:dd/MM/yyyy} | Active Sales: {activeSales}";
                pnlLeaseBox.BackColor = ThemeColors.PrimaryLight;
            }
            else if (activeSales > 0)
            {
                lblLeaseProp.Text = $"Active Installment Deals: {activeSales}";
                lblLeaseRent.Text = $"Credit Limit: {SettingService.FormatCurrency(_selectedTenant.CreditLimit)}";
                lblLeasePeriod.Text = "Retail / Installment Customer";
                pnlLeaseBox.BackColor = ThemeColors.PrimaryLight;
            }
            else
            {
                lblLeaseProp.Text = "No Active Contract";
                lblLeaseRent.Text = "No active agreements or installment plans";
                lblLeasePeriod.Text = "-";
                pnlLeaseBox.BackColor = Color.FromArgb(241, 245, 249);
            }

            decimal bal = TenantService.GetTenantCurrentBalance(_selectedTenant.Id);
            lblBalance.Text = SettingService.FormatCurrency(bal);
            lblBalance.ForeColor = bal > 0 ? ThemeColors.Danger : (bal < 0 ? ThemeColors.InfoText : ThemeColors.SuccessText);
        }

        private void ClearTenantDetails()
        {
            lblTenantName.Text = "Select a Record";
            lblTenantMeta.Text = "Code: - | Type: -";
            lblContact.Text = "Phone: -";
            lblCnic.Text = "CNIC: -";
            lblAddress.Text = "Address: -";
            lblGuarantor.Text = "Guarantor: -";
            lblLeaseProp.Text = "Unit / Account: None";
            lblLeaseRent.Text = "Rate / Plan: -";
            lblLeasePeriod.Text = "-";
            lblBalance.Text = "Rs. 0";
        }

        private void BtnAddTenant_Click(object? sender, EventArgs e)
        {
            using var dlg = new TenantEditForm();
            if (dlg.ShowDialog() == DialogResult.OK) LoadTenants();
        }

        private void BtnEditTenant_Click(object? sender, EventArgs e)
        {
            if (_selectedTenant == null) return;
            using var dlg = new TenantEditForm(_selectedTenant);
            if (dlg.ShowDialog() == DialogResult.OK) LoadTenants();
        }

        private void BtnArchiveTenant_Click(object? sender, EventArgs e)
        {
            if (_selectedTenant == null) return;
            if (ModernMessageBox.ShowConfirm($"Are you sure you want to delete or archive '{_selectedTenant.FullName}'?", "Confirm Delete/Archive", this))
            {
                var res = TenantService.DeleteOrArchiveTenant(_selectedTenant.Id);
                ModernMessageBox.ShowInfo(res.Message, "Result", this);
                LoadTenants();
            }
        }

        private void BtnPay_Click(object? sender, EventArgs e)
        {
            if (_selectedTenant == null)
            {
                ModernMessageBox.ShowInfo("Please select a record first.", "Selection Required", this);
                return;
            }

            using var dlg = new RecordPaymentForm(_selectedTenant.Id);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadTenants();
            }
        }

        private void BtnNewLease_Click(object? sender, EventArgs e)
        {
            if (_selectedTenant == null)
            {
                ModernMessageBox.ShowInfo("Please select a record first.", "Selection Required", this);
                return;
            }

            using var dlg = new RentAgreementForm(null, _selectedTenant.Id);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadTenants();
            }
        }

        private void BtnPrintStatement_Click(object? sender, EventArgs e)
        {
            if (_selectedTenant == null) return;
            var ds = ReportService.GetTenantStatementReport(_selectedTenant.Id);
            PrintingService.PrintReport(ds, showPreview: true);
        }
    }
}
