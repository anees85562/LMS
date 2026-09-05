using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class RentAgreementForm : Form
    {
        private RentAgreement _agreement;
        private ComboBox cmbTenant = null!;
        private ComboBox cmbProperty = null!;
        private ComboBox cmbUnit = null!;
        private DateTimePicker dtpStart = null!;
        private DateTimePicker dtpEnd = null!;
        private CheckBox chkPeriodic = null!;
        private TextBox txtRent = null!;
        private TextBox txtSecurity = null!;
        private TextBox txtAdvance = null!;
        private NumericUpDown numDueDay = null!;
        private NumericUpDown numIncrement = null!;
        private CheckBox chkPostDeposits = null!;
        private TextBox txtRemarks = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        private List<Tenant> _tenants = new();
        private List<Property> _properties = new();
        private List<PropertyUnit> _currentUnits = new();

        public RentAgreementForm(RentAgreement? agreement = null, int? preselectedTenantId = null, int? preselectedUnitId = null)
        {
            _agreement = agreement ?? new RentAgreement
            {
                AgreementCode = LeaseService.GenerateNextAgreementCode(),
                StartDate = DateTime.Now.Date,
                EndDate = DateTime.Now.Date.AddYears(1),
                DueDayOfMonth = SettingService.GetInt("Rent.DefaultDueDay", 5),
                RentIncrementRatePercent = 10,
                MonthlyRent = 30000,
                SecurityDeposit = 60000
            };

            if (preselectedTenantId.HasValue) _agreement.TenantId = preselectedTenantId.Value;
            if (preselectedUnitId.HasValue) _agreement.PropertyUnitId = preselectedUnitId.Value;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = _agreement.Id == 0 ? "Create Rent / Lease Agreement" : "Edit Rent Agreement";
            Size = new Size(680, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _agreement.Id == 0 ? "📝 Create Rent / Lease Agreement" : "📝 Edit Rent Agreement",
                "Assign property unit/shop to tenant, configure lease terms, monthly rent, deposits, and due dates"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 12,
                Padding = new Padding(0),
                AutoScroll = true
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));

            for (int i = 0; i < 11; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Tenant
            mainPanel.Controls.Add(CreateFieldLabel("Select Tenant *"), 0, 0);
            cmbTenant = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            mainPanel.Controls.Add(cmbTenant, 1, 0);

            // Property
            mainPanel.Controls.Add(CreateFieldLabel("Select Property *"), 0, 1);
            cmbProperty = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            cmbProperty.SelectedIndexChanged += CmbProperty_SelectedIndexChanged;
            mainPanel.Controls.Add(cmbProperty, 1, 1);

            // Property Unit
            mainPanel.Controls.Add(CreateFieldLabel("Select Unit / Shop *"), 0, 2);
            cmbUnit = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            cmbUnit.SelectedIndexChanged += CmbUnit_SelectedIndexChanged;
            mainPanel.Controls.Add(cmbUnit, 1, 2);

            // Start Date
            mainPanel.Controls.Add(CreateFieldLabel("Lease Start Date *"), 0, 3);
            dtpStart = new DateTimePicker { Dock = DockStyle.Left, Width = 180, Height = 28, Format = DateTimePickerFormat.Short, Value = _agreement.StartDate };
            mainPanel.Controls.Add(dtpStart, 1, 3);

            // End Date & Periodic
            mainPanel.Controls.Add(CreateFieldLabel("Lease End Date"), 0, 4);
            var endFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            dtpEnd = new DateTimePicker { Width = 150, Height = 28, Format = DateTimePickerFormat.Short, Value = _agreement.EndDate ?? DateTime.Now.AddYears(1) };
            chkPeriodic = new CheckBox { Text = "Periodic (No End Date)", AutoSize = true, Margin = new Padding(10, 4, 0, 0), Font = ThemeColors.BodyFont, Checked = !_agreement.EndDate.HasValue, ForeColor = ThemeColors.TextPrimary };
            chkPeriodic.CheckedChanged += (s, e) => { dtpEnd.Enabled = !chkPeriodic.Checked; };
            endFlow.Controls.Add(dtpEnd);
            endFlow.Controls.Add(chkPeriodic);
            mainPanel.Controls.Add(endFlow, 1, 4);

            // Monthly Rent
            mainPanel.Controls.Add(CreateFieldLabel("Monthly Rent (Rs.) *"), 0, 5);
            txtRent = new TextBox { Text = _agreement.MonthlyRent.ToString("N0"), Dock = DockStyle.Left, Width = 180, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = ThemeColors.Primary };
            mainPanel.Controls.Add(txtRent, 1, 5);

            // Security Deposit
            mainPanel.Controls.Add(CreateFieldLabel("Security Deposit (Rs.)"), 0, 6);
            txtSecurity = new TextBox { Text = _agreement.SecurityDeposit.ToString("N0"), Dock = DockStyle.Left, Width = 180, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtSecurity, 1, 6);

            // Advance Rent
            mainPanel.Controls.Add(CreateFieldLabel("Advance Rent (Rs.)"), 0, 7);
            txtAdvance = new TextBox { Text = _agreement.AdvanceAmount.ToString("N0"), Dock = DockStyle.Left, Width = 180, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtAdvance, 1, 7);

            // Rent Due Day
            mainPanel.Controls.Add(CreateFieldLabel("Rent Due Day of Month"), 0, 8);
            var dueFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            numDueDay = new NumericUpDown { Minimum = 1, Maximum = 31, Value = _agreement.DueDayOfMonth, Width = 80, Height = 28, Font = ThemeColors.BodyFont };
            var lblDueHelp = new Label { Text = "(e.g. 5th of every month)", AutoSize = true, Margin = new Padding(8, 4, 0, 0), ForeColor = ThemeColors.TextMuted, Font = ThemeColors.SmallFont };
            dueFlow.Controls.Add(numDueDay);
            dueFlow.Controls.Add(lblDueHelp);
            mainPanel.Controls.Add(dueFlow, 1, 8);

            // Annual Rent Increment %
            mainPanel.Controls.Add(CreateFieldLabel("Annual Increment %"), 0, 9);
            numIncrement = new NumericUpDown { Minimum = 0, Maximum = 100, Value = _agreement.RentIncrementRatePercent, Dock = DockStyle.Left, Width = 80, Height = 28, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(numIncrement, 1, 9);

            // Checkbox for initial deposit posting
            mainPanel.Controls.Add(CreateFieldLabel("Accounting Options"), 0, 10);
            chkPostDeposits = new CheckBox
            {
                Text = "Post initial Security Deposit & Advance to ledger automatically",
                Dock = DockStyle.Fill,
                Checked = true,
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.Primary
            };
            mainPanel.Controls.Add(chkPostDeposits, 1, 10);

            // Remarks / Terms
            mainPanel.Controls.Add(CreateFieldLabel("Terms / Remarks"), 0, 11);
            txtRemarks = new TextBox { Text = _agreement.Remarks, Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtRemarks, 1, 11);

            pnlCard.Controls.Add(mainPanel);

            btnSave = new ModernButton
            {
                Text = "✓ Create Agreement",
                StyleType = ButtonStyleType.Primary,
                Width = 170,
                Height = 38
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var pnlDialogFooter = UIHelper.CreateDialogFooter(btnSave, btnCancel);

            var containerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
            containerPanel.Controls.Add(pnlCard);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);

            Load += RentAgreementForm_Load;
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void RentAgreementForm_Load(object? sender, EventArgs e)
        {
            _tenants = TenantService.GetAllTenants(TenantStatus.Active);
            cmbTenant.DisplayMember = "FullName";
            cmbTenant.ValueMember = "Id";
            cmbTenant.DataSource = _tenants;

            if (_agreement.TenantId > 0)
            {
                cmbTenant.SelectedValue = _agreement.TenantId;
            }

            _properties = PropertyService.GetAllProperties();
            cmbProperty.DisplayMember = "Name";
            cmbProperty.ValueMember = "Id";
            cmbProperty.DataSource = _properties;

            if (_agreement.PropertyUnitId > 0)
            {
                var unit = PropertyService.GetAllUnits().FirstOrDefault(u => u.Id == _agreement.PropertyUnitId);
                if (unit != null)
                {
                    cmbProperty.SelectedValue = unit.PropertyId;
                    cmbUnit.SelectedValue = unit.Id;
                }
            }
        }

        private void CmbProperty_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbProperty.SelectedValue is int propId && propId > 0)
            {
                _currentUnits = PropertyService.GetUnitsByPropertyId(propId);
                cmbUnit.DisplayMember = "UnitNumber";
                cmbUnit.ValueMember = "Id";
                cmbUnit.DataSource = _currentUnits;

                if (_agreement.PropertyUnitId > 0)
                {
                    cmbUnit.SelectedValue = _agreement.PropertyUnitId;
                }
            }
        }

        private void CmbUnit_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbUnit.SelectedItem is PropertyUnit unit && unit.BaseRent > 0)
            {
                txtRent.Text = unit.BaseRent.ToString("N0");
                txtSecurity.Text = (unit.BaseRent * 2).ToString("N0"); // Default 2 months security
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbTenant.SelectedValue is not int tenantId || tenantId <= 0)
            {
                ModernMessageBox.ShowWarning("Please select a tenant.", "Validation", this);
                return;
            }

            if (cmbUnit.SelectedValue is not int unitId || unitId <= 0)
            {
                ModernMessageBox.ShowWarning("Please select a property unit.", "Validation", this);
                return;
            }

            decimal.TryParse(txtRent.Text.Replace(",", ""), out decimal rent);
            if (rent <= 0)
            {
                ModernMessageBox.ShowWarning("Please enter a valid monthly rent amount.", "Validation", this);
                return;
            }

            decimal.TryParse(txtSecurity.Text.Replace(",", ""), out decimal security);
            decimal.TryParse(txtAdvance.Text.Replace(",", ""), out decimal advance);

            _agreement.TenantId = tenantId;
            _agreement.PropertyUnitId = unitId;
            _agreement.StartDate = dtpStart.Value.Date;
            _agreement.EndDate = chkPeriodic.Checked ? null : dtpEnd.Value.Date;
            _agreement.MonthlyRent = rent;
            _agreement.SecurityDeposit = security;
            _agreement.AdvanceAmount = advance;
            _agreement.DueDayOfMonth = (int)numDueDay.Value;
            _agreement.RentIncrementRatePercent = numIncrement.Value;
            _agreement.Remarks = txtRemarks.Text.Trim();

            var res = LeaseService.CreateAgreement(_agreement, chkPostDeposits.Checked);
            if (!res.Success)
            {
                ModernMessageBox.ShowError(res.Message, "Error", this);
                return;
            }

            ModernMessageBox.ShowInfo("Rent agreement created successfully!\nThe unit has been marked as Occupied.", "Success", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
