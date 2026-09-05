using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class PropertyEditForm : Form
    {
        private Property _property;
        private TextBox txtCode = null!;
        private TextBox txtName = null!;
        private ComboBox cmbType = null!;
        private TextBox txtAddress = null!;
        private TextBox txtCity = null!;
        private ComboBox cmbStatus = null!;
        private TextBox txtNotes = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public PropertyEditForm(Property? property = null)
        {
            _property = property ?? new Property
            {
                PropertyCode = PropertyService.GenerateNextPropertyCode(),
                PropertyType = "Residential",
                Status = PropertyStatus.Active
            };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = _property.Id == 0 ? "Add New Property" : "Edit Property Details";
            Size = new Size(580, 530);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _property.Id == 0 ? "🏢 Add New Property / Building" : "🏢 Edit Property Details",
                "Define property name, address, property type, and occupancy status"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(0)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            for (int i = 0; i < 6; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Property Code
            mainPanel.Controls.Add(CreateFieldLabel("Property Code"), 0, 0);
            txtCode = CreateStyledTextBox();
            txtCode.Text = _property.PropertyCode;
            mainPanel.Controls.Add(txtCode, 1, 0);

            // Name
            mainPanel.Controls.Add(CreateFieldLabel("Property Name *"), 0, 1);
            txtName = CreateStyledTextBox();
            txtName.Text = _property.Name;
            mainPanel.Controls.Add(txtName, 1, 1);

            // Type
            mainPanel.Controls.Add(CreateFieldLabel("Property Type"), 0, 2);
            cmbType = new ComboBox { Dock = DockStyle.Fill, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Residential", "Commercial", "Plaza", "Building", "House", "Shop", "Flat", "Warehouse", "Plot" });
            cmbType.SelectedItem = _property.PropertyType ?? "Residential";
            mainPanel.Controls.Add(cmbType, 1, 2);

            // Address
            mainPanel.Controls.Add(CreateFieldLabel("Address"), 0, 3);
            txtAddress = CreateStyledTextBox();
            txtAddress.Text = _property.Address;
            mainPanel.Controls.Add(txtAddress, 1, 3);

            // City
            mainPanel.Controls.Add(CreateFieldLabel("City"), 0, 4);
            txtCity = CreateStyledTextBox();
            txtCity.Text = _property.City ?? "Lahore";
            mainPanel.Controls.Add(txtCity, 1, 4);

            // Status
            mainPanel.Controls.Add(CreateFieldLabel("Status"), 0, 5);
            cmbStatus = new ComboBox { Dock = DockStyle.Fill, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { PropertyStatus.Active, PropertyStatus.Inactive, PropertyStatus.Archived });
            cmbStatus.SelectedItem = _property.Status;
            mainPanel.Controls.Add(cmbStatus, 1, 5);

            // Notes
            mainPanel.Controls.Add(CreateFieldLabel("Notes"), 0, 6);
            txtNotes = new TextBox { Text = _property.Notes, Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtNotes, 1, 6);

            pnlCard.Controls.Add(mainPanel);

            btnSave = new ModernButton
            {
                Text = "✓ Save Property",
                StyleType = ButtonStyleType.Primary,
                Width = 140,
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
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox { Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                ModernMessageBox.ShowWarning("Please enter a property name.", "Validation", this);
                return;
            }

            _property.PropertyCode = txtCode.Text.Trim();
            _property.Name = txtName.Text.Trim();
            _property.PropertyType = cmbType.SelectedItem?.ToString() ?? "Residential";
            _property.Address = txtAddress.Text.Trim();
            _property.City = txtCity.Text.Trim();
            _property.Status = (PropertyStatus)(cmbStatus.SelectedItem ?? PropertyStatus.Active);
            _property.Notes = txtNotes.Text.Trim();

            var res = PropertyService.SaveProperty(_property);
            if (!res.Success)
            {
                ModernMessageBox.ShowError(res.Message, "Error", this);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public class UnitEditForm : Form
    {
        private PropertyUnit _unit;
        private int _propertyId;
        private TextBox txtNumber = null!;
        private ComboBox cmbType = null!;
        private TextBox txtFloor = null!;
        private TextBox txtRent = null!;
        private ComboBox cmbStatus = null!;
        private TextBox txtNotes = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public UnitEditForm(int propertyId, PropertyUnit? unit = null)
        {
            _propertyId = propertyId;
            _unit = unit ?? new PropertyUnit
            {
                PropertyId = propertyId,
                UnitType = "Portion",
                BaseRent = 25000,
                Status = UnitStatus.Vacant
            };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = _unit.Id == 0 ? "Add Unit / Portion" : "Edit Unit Details";
            Size = new Size(540, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _unit.Id == 0 ? "🚪 Add Property Unit / Shop / Portion" : "🚪 Edit Unit Details",
                "Configure unit designation, type, floor level, expected rent, and status"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(0)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));

            for (int i = 0; i < 5; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Unit Number
            mainPanel.Controls.Add(CreateFieldLabel("Unit # / Name *"), 0, 0);
            txtNumber = new TextBox { Text = _unit.UnitNumber, Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtNumber, 1, 0);

            // Type
            mainPanel.Controls.Add(CreateFieldLabel("Unit Type"), 0, 1);
            cmbType = new ComboBox { Dock = DockStyle.Fill, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Portion", "Shop", "Flat", "Room", "Floor", "Office", "Warehouse", "House" });
            cmbType.SelectedItem = _unit.UnitType ?? "Portion";
            mainPanel.Controls.Add(cmbType, 1, 1);

            // Floor
            mainPanel.Controls.Add(CreateFieldLabel("Floor / Level"), 0, 2);
            txtFloor = new TextBox { Text = _unit.Floor ?? "Ground", Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtFloor, 1, 2);

            // Base Rent
            mainPanel.Controls.Add(CreateFieldLabel("Base Rent (Rs.)"), 0, 3);
            txtRent = new TextBox { Text = _unit.BaseRent.ToString("N0"), Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = ThemeColors.Primary };
            mainPanel.Controls.Add(txtRent, 1, 3);

            // Status
            mainPanel.Controls.Add(CreateFieldLabel("Occupancy Status"), 0, 4);
            cmbStatus = new ComboBox { Dock = DockStyle.Fill, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { UnitStatus.Vacant, UnitStatus.Occupied, UnitStatus.UnderMaintenance });
            cmbStatus.SelectedItem = _unit.Status;
            mainPanel.Controls.Add(cmbStatus, 1, 4);

            // Notes
            mainPanel.Controls.Add(CreateFieldLabel("Notes"), 0, 5);
            txtNotes = new TextBox { Text = _unit.Notes, Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtNotes, 1, 5);

            pnlCard.Controls.Add(mainPanel);

            btnSave = new ModernButton
            {
                Text = "✓ Save Unit",
                StyleType = ButtonStyleType.Primary,
                Width = 130,
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
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNumber.Text))
            {
                ModernMessageBox.ShowWarning("Please enter a unit number or name.", "Validation", this);
                return;
            }

            decimal.TryParse(txtRent.Text.Replace(",", ""), out decimal rent);

            _unit.PropertyId = _propertyId;
            _unit.UnitNumber = txtNumber.Text.Trim();
            _unit.UnitType = cmbType.SelectedItem?.ToString() ?? "Portion";
            _unit.Floor = txtFloor.Text.Trim();
            _unit.BaseRent = rent;
            _unit.Status = (UnitStatus)(cmbStatus.SelectedItem ?? UnitStatus.Vacant);
            _unit.Notes = txtNotes.Text.Trim();

            var res = PropertyService.SaveUnit(_unit);
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
