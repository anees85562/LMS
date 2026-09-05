using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class TenantEditForm : Form
    {
        private Tenant _tenant;
        private TextBox txtCode = null!;
        private TextBox txtName = null!;
        private ComboBox cmbCustomerType = null!;
        private TextBox txtFather = null!;
        private TextBox txtCnic = null!;
        private TextBox txtPhone = null!;
        private TextBox txtAltPhone = null!;
        private TextBox txtCity = null!;
        private TextBox txtAddress = null!;
        private NumericUpDown numCreditLimit = null!;
        private ComboBox cmbRating = null!;
        private ComboBox cmbStatus = null!;

        // Guarantor
        private TextBox txtGuarantorName = null!;
        private TextBox txtGuarantorPhone = null!;
        private TextBox txtGuarantorCnic = null!;
        private TextBox txtGuarantorRelation = null!;
        private TextBox txtGuarantorAddress = null!;

        private TextBox txtNotes = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public TenantEditForm(Tenant? tenant = null, CustomerType defaultType = CustomerType.Tenant)
        {
            _tenant = tenant ?? new Tenant
            {
                CustomerType = defaultType,
                TenantCode = TenantService.GenerateNextTenantCode(defaultType),
                Status = TenantStatus.Active,
                Rating = "Good"
            };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = _tenant.Id == 0 ? "Add New Customer / Party / Tenant" : "Edit Customer / Tenant Details";
            Size = new Size(740, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _tenant.Id == 0 ? "👤 Add New Customer / Party / Tenant" : "👤 Edit Customer / Tenant Profile",
                "Enter personal information, contact numbers, CNIC, risk rating, credit limits, and guarantor details"
            );

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ThemeColors.BodyFont,
                Padding = new Point(12, 6)
            };

            // Tab 1: General Info
            var tabGeneral = new TabPage { Text = "General Information", BackColor = Color.White, Padding = new Padding(16, 12, 16, 12) };
            var genLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 8,
                Padding = new Padding(0),
                AutoScroll = true
            };
            genLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            genLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            genLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            genLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            for (int i = 0; i < 7; i++)
                genLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            genLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Row 0: Customer Type & Code
            genLayout.Controls.Add(CreateFieldLabel("Party Type *"), 0, 0);
            cmbCustomerType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            cmbCustomerType.Items.Add(CustomerType.Tenant);
            cmbCustomerType.Items.Add(CustomerType.InstallmentCustomer);
            cmbCustomerType.Items.Add(CustomerType.BNPLCreditCustomer);
            cmbCustomerType.Items.Add(CustomerType.GeneralParty);
            cmbCustomerType.SelectedItem = _tenant.CustomerType;
            cmbCustomerType.SelectedIndexChanged += (s, e) =>
            {
                if (_tenant.Id == 0 && cmbCustomerType.SelectedItem is CustomerType ct)
                {
                    txtCode.Text = TenantService.GenerateNextTenantCode(ct);
                }
            };
            genLayout.Controls.Add(cmbCustomerType, 1, 0);

            genLayout.Controls.Add(CreateFieldLabel("Code / Reg #"), 2, 0);
            txtCode = CreateStyledTextBox();
            txtCode.Text = _tenant.TenantCode;
            genLayout.Controls.Add(txtCode, 3, 0);

            // Row 1: Full Name
            genLayout.Controls.Add(CreateFieldLabel("Full Name *"), 0, 1);
            txtName = CreateStyledTextBox();
            txtName.Text = _tenant.FullName;
            genLayout.Controls.Add(txtName, 1, 1);
            genLayout.SetColumnSpan(txtName, 3);

            // Row 2: Father Name
            genLayout.Controls.Add(CreateFieldLabel("Father / Husband"), 0, 2);
            txtFather = CreateStyledTextBox();
            txtFather.Text = _tenant.FatherOrHusbandName;
            genLayout.Controls.Add(txtFather, 1, 2);
            genLayout.SetColumnSpan(txtFather, 3);

            // Row 3: CNIC & City
            genLayout.Controls.Add(CreateFieldLabel("CNIC / ID Number"), 0, 3);
            txtCnic = CreateStyledTextBox();
            txtCnic.Text = _tenant.CnicOrId;
            genLayout.Controls.Add(txtCnic, 1, 3);

            genLayout.Controls.Add(CreateFieldLabel("City"), 2, 3);
            txtCity = CreateStyledTextBox();
            txtCity.Text = _tenant.City;
            genLayout.Controls.Add(txtCity, 3, 3);

            // Row 4: Primary Mobile & Alt Phone
            genLayout.Controls.Add(CreateFieldLabel("Primary Mobile *"), 0, 4);
            txtPhone = CreateStyledTextBox();
            txtPhone.Text = _tenant.ContactNumber;
            genLayout.Controls.Add(txtPhone, 1, 4);

            genLayout.Controls.Add(CreateFieldLabel("Alt Phone"), 2, 4);
            txtAltPhone = CreateStyledTextBox();
            txtAltPhone.Text = _tenant.AlternateContact;
            genLayout.Controls.Add(txtAltPhone, 3, 4);

            // Row 5: Address
            genLayout.Controls.Add(CreateFieldLabel("Address"), 0, 5);
            txtAddress = CreateStyledTextBox();
            txtAddress.Text = _tenant.PermanentAddress;
            genLayout.Controls.Add(txtAddress, 1, 5);
            genLayout.SetColumnSpan(txtAddress, 3);

            // Row 6: Credit Limit & Rating & Status
            genLayout.Controls.Add(CreateFieldLabel("Credit Limit"), 0, 6);
            numCreditLimit = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, Value = _tenant.CreditLimit, Dock = DockStyle.Fill, Height = 28, Font = ThemeColors.BodyFont };
            genLayout.Controls.Add(numCreditLimit, 1, 6);

            genLayout.Controls.Add(CreateFieldLabel("Risk / Status"), 2, 6);
            var statusFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            cmbRating = new ComboBox { Width = 90, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRating.Items.AddRange(new object[] { "Good", "Fair", "Risky", "Defaulter" });
            cmbRating.SelectedItem = _tenant.Rating ?? "Good";

            cmbStatus = new ComboBox { Width = 95, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { TenantStatus.Active, TenantStatus.Previous, TenantStatus.Evicted, TenantStatus.Blacklisted });
            cmbStatus.SelectedItem = _tenant.Status;

            statusFlow.Controls.Add(cmbRating);
            statusFlow.Controls.Add(cmbStatus);
            genLayout.Controls.Add(statusFlow, 3, 6);

            // Row 7: Notes
            genLayout.Controls.Add(CreateFieldLabel("Notes / Remarks"), 0, 7);
            txtNotes = new TextBox { Text = _tenant.Notes, Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            genLayout.Controls.Add(txtNotes, 1, 7);
            genLayout.SetColumnSpan(txtNotes, 3);

            tabGeneral.Controls.Add(genLayout);
            tabControl.TabPages.Add(tabGeneral);

            // Tab 2: Guarantor & References
            var tabGuarantor = new TabPage { Text = "Guarantor / Reference Info", BackColor = Color.White, Padding = new Padding(16, 12, 16, 12) };
            var guarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(0)
            };
            guarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            guarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            guarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            guarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            for (int i = 0; i < 4; i++)
                guarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            guarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            guarLayout.Controls.Add(CreateFieldLabel("Guarantor Name"), 0, 0);
            txtGuarantorName = CreateStyledTextBox();
            txtGuarantorName.Text = _tenant.GuarantorName;
            guarLayout.Controls.Add(txtGuarantorName, 1, 0);
            guarLayout.SetColumnSpan(txtGuarantorName, 3);

            guarLayout.Controls.Add(CreateFieldLabel("Guarantor Phone"), 0, 1);
            txtGuarantorPhone = CreateStyledTextBox();
            txtGuarantorPhone.Text = _tenant.GuarantorPhone;
            guarLayout.Controls.Add(txtGuarantorPhone, 1, 1);

            guarLayout.Controls.Add(CreateFieldLabel("Relation"), 2, 1);
            txtGuarantorRelation = CreateStyledTextBox();
            txtGuarantorRelation.Text = _tenant.GuarantorRelation;
            txtGuarantorRelation.PlaceholderText = "e.g. Brother, Friend";
            guarLayout.Controls.Add(txtGuarantorRelation, 3, 1);

            guarLayout.Controls.Add(CreateFieldLabel("Guarantor CNIC"), 0, 2);
            txtGuarantorCnic = CreateStyledTextBox();
            txtGuarantorCnic.Text = _tenant.GuarantorCnic;
            guarLayout.Controls.Add(txtGuarantorCnic, 1, 2);
            guarLayout.SetColumnSpan(txtGuarantorCnic, 3);

            guarLayout.Controls.Add(CreateFieldLabel("Guarantor Address"), 0, 3);
            txtGuarantorAddress = new TextBox { Text = _tenant.GuarantorAddress, Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            guarLayout.Controls.Add(txtGuarantorAddress, 1, 3);
            guarLayout.SetColumnSpan(txtGuarantorAddress, 3);

            tabGuarantor.Controls.Add(guarLayout);
            tabControl.TabPages.Add(tabGuarantor);

            btnSave = new ModernButton
            {
                Text = "✓ Save Details",
                StyleType = ButtonStyleType.Primary,
                Width = 150,
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
            containerPanel.Controls.Add(tabControl);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeColors.BodyFont
            };
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ModernMessageBox.ShowWarning("Please enter full name.", "Validation", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                ModernMessageBox.ShowWarning("Please enter contact mobile number.", "Validation", this);
                return;
            }

            _tenant.CustomerType = (CustomerType)(cmbCustomerType.SelectedItem ?? CustomerType.Tenant);
            _tenant.TenantCode = txtCode.Text.Trim();
            _tenant.FullName = txtName.Text.Trim();
            _tenant.FatherOrHusbandName = txtFather.Text.Trim();
            _tenant.CnicOrId = txtCnic.Text.Trim();
            _tenant.City = txtCity.Text.Trim();
            _tenant.ContactNumber = txtPhone.Text.Trim();
            _tenant.AlternateContact = txtAltPhone.Text.Trim();
            _tenant.PermanentAddress = txtAddress.Text.Trim();
            _tenant.CreditLimit = numCreditLimit.Value;
            _tenant.Rating = cmbRating.SelectedItem?.ToString() ?? "Good";
            _tenant.Status = (TenantStatus)(cmbStatus.SelectedItem ?? TenantStatus.Active);
            _tenant.Notes = txtNotes.Text.Trim();

            // Guarantor
            _tenant.GuarantorName = txtGuarantorName.Text.Trim();
            _tenant.GuarantorPhone = txtGuarantorPhone.Text.Trim();
            _tenant.GuarantorCnic = txtGuarantorCnic.Text.Trim();
            _tenant.GuarantorRelation = txtGuarantorRelation.Text.Trim();
            _tenant.GuarantorAddress = txtGuarantorAddress.Text.Trim();

            var res = TenantService.SaveTenant(_tenant);
            if (!res.Success)
            {
                ModernMessageBox.ShowError(res.Message, "Error", this);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
