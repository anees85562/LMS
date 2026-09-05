using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class SettingsView : UserControl
    {
        private ComboBox cmbBusinessType = null!;
        private TextBox txtCompanyName = null!;
        private TextBox txtOwnerName = null!;
        private TextBox txtPhone = null!;
        private TextBox txtAddress = null!;
        private TextBox txtCurrency = null!;
        private TextBox txtDateFormat = null!;

        // Retail & Installment settings
        private TextBox txtInvoicePrefix = null!;
        private TextBox txtProductCodePrefix = null!;
        private NumericUpDown numGracePeriodDays = null!;
        private NumericUpDown numDefaultLateFee = null!;

        // Rent settings
        private NumericUpDown numDefaultDueDay = null!;
        private NumericUpDown numReminderDays = null!;
        private NumericUpDown numOverdueDays = null!;
        private NumericUpDown numExpiryAlertDays = null!;

        // Backup
        private CheckBox chkAutoBackupOnExit = null!;
        private NumericUpDown numRetentionDays = null!;

        // Receipt
        private TextBox txtReceiptPrefix = null!;
        private TextBox txtReceiptHeader = null!;
        private TextBox txtReceiptFooter = null!;

        private NumericUpDown numAutoLockMinutes = null!;
        private ModernButton btnSaveSettings = null!;

        public SettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;
            AutoScroll = true;

            var pnlHeader = UIHelper.CreatePageHeader(
                "⚙️ Application Settings & Business Rules",
                "Configure business operating mode (Installments, BNPL, Property Rent, Mixed), invoice prefixes, alert thresholds, receipt templates, and backups."
            );

            var scrollContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 24),
                BackColor = ThemeColors.CanvasBg
            };

            var mainCard = UIHelper.CreateCardPanel(new Padding(24, 20, 24, 24));
            mainCard.Dock = DockStyle.Top;
            mainCard.AutoSize = true;
            mainCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 28,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            int row = 0;

            // SECTION 1
            AddSectionHeader(layout, "🏢 1. Business Mode & Organization Profile", ref row);

            layout.Controls.Add(CreateFieldLabel("Operating Mode *"), 0, row);
            cmbBusinessType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            cmbBusinessType.Items.Add(BusinessType.Mixed);
            cmbBusinessType.Items.Add(BusinessType.InstallmentRetail);
            cmbBusinessType.Items.Add(BusinessType.PropertyRent);
            cmbBusinessType.Items.Add(BusinessType.BNPL);
            cmbBusinessType.Items.Add(BusinessType.GeneralReceivables);
            layout.Controls.Add(cmbBusinessType, 1, row);

            layout.Controls.Add(CreateFieldLabel("Currency Symbol"), 2, row);
            txtCurrency = CreateStyledTextBox();
            layout.Controls.Add(txtCurrency, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Business / Plaza Name"), 0, row);
            txtCompanyName = CreateStyledTextBox();
            layout.Controls.Add(txtCompanyName, 1, row);

            layout.Controls.Add(CreateFieldLabel("Date Format"), 2, row);
            txtDateFormat = CreateStyledTextBox();
            layout.Controls.Add(txtDateFormat, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Owner Full Name"), 0, row);
            txtOwnerName = CreateStyledTextBox();
            layout.Controls.Add(txtOwnerName, 1, row);

            layout.Controls.Add(CreateFieldLabel("Auto-Lock (mins)"), 2, row);
            numAutoLockMinutes = new NumericUpDown { Minimum = 0, Maximum = 120, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numAutoLockMinutes, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Office Phone"), 0, row);
            txtPhone = CreateStyledTextBox();
            layout.Controls.Add(txtPhone, 1, row);

            layout.Controls.Add(CreateFieldLabel("Office Address"), 2, row);
            txtAddress = CreateStyledTextBox();
            layout.Controls.Add(txtAddress, 3, row);
            row++;

            AddSpacer(layout, ref row);

            // SECTION 2
            AddSectionHeader(layout, "🛒 2. Installment, Retail & BNPL Settings", ref row);

            layout.Controls.Add(CreateFieldLabel("Invoice Prefix"), 0, row);
            txtInvoicePrefix = CreateStyledTextBox();
            layout.Controls.Add(txtInvoicePrefix, 1, row);

            layout.Controls.Add(CreateFieldLabel("Product Code Prefix"), 2, row);
            txtProductCodePrefix = CreateStyledTextBox();
            layout.Controls.Add(txtProductCodePrefix, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Grace Period (Days)"), 0, row);
            numGracePeriodDays = new NumericUpDown { Minimum = 0, Maximum = 30, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numGracePeriodDays, 1, row);

            layout.Controls.Add(CreateFieldLabel("Default Late Fee (%)"), 2, row);
            numDefaultLateFee = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 2, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numDefaultLateFee, 3, row);
            row++;

            AddSpacer(layout, ref row);

            // SECTION 3
            AddSectionHeader(layout, "🔔 3. Rental Rules & Due Notification Thresholds", ref row);

            layout.Controls.Add(CreateFieldLabel("Default Due Day"), 0, row);
            numDefaultDueDay = new NumericUpDown { Minimum = 1, Maximum = 31, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numDefaultDueDay, 1, row);

            layout.Controls.Add(CreateFieldLabel("Upcoming Alert (Days)"), 2, row);
            numReminderDays = new NumericUpDown { Minimum = 0, Maximum = 30, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numReminderDays, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Overdue Alert After (Days)"), 0, row);
            numOverdueDays = new NumericUpDown { Minimum = 0, Maximum = 30, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numOverdueDays, 1, row);

            layout.Controls.Add(CreateFieldLabel("Lease Expiry Alert (Days)"), 2, row);
            numExpiryAlertDays = new NumericUpDown { Minimum = 0, Maximum = 90, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numExpiryAlertDays, 3, row);
            row++;

            AddSpacer(layout, ref row);

            // SECTION 4
            AddSectionHeader(layout, "🧾 4. Payment Receipt Template", ref row);

            layout.Controls.Add(CreateFieldLabel("Receipt Prefix"), 0, row);
            txtReceiptPrefix = CreateStyledTextBox();
            layout.Controls.Add(txtReceiptPrefix, 1, row);

            layout.Controls.Add(CreateFieldLabel("Receipt Header"), 2, row);
            txtReceiptHeader = CreateStyledTextBox();
            layout.Controls.Add(txtReceiptHeader, 3, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Receipt Footer Note"), 0, row);
            txtReceiptFooter = CreateStyledTextBox();
            layout.SetColumnSpan(txtReceiptFooter, 3);
            layout.Controls.Add(txtReceiptFooter, 1, row);
            row++;

            AddSpacer(layout, ref row);

            // SECTION 5
            AddSectionHeader(layout, "💾 5. Automated Backup Policies", ref row);

            chkAutoBackupOnExit = new CheckBox
            {
                Text = "Automatically create a timestamped database backup on application exit",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.TextPrimary
            };
            layout.SetColumnSpan(chkAutoBackupOnExit, 4);
            layout.Controls.Add(chkAutoBackupOnExit, 0, row);
            row++;

            layout.Controls.Add(CreateFieldLabel("Retention Period (Days)"), 0, row);
            numRetentionDays = new NumericUpDown { Minimum = 1, Maximum = 365, Dock = DockStyle.Left, Width = 120, Height = 28 };
            layout.Controls.Add(numRetentionDays, 1, row);

            var lblRetHelp = new Label
            {
                Text = "(Backups older than retention days are safely pruned on schedule)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeColors.TextMuted,
                Font = ThemeColors.SmallFont
            };
            layout.SetColumnSpan(lblRetHelp, 2);
            layout.Controls.Add(lblRetHelp, 2, row);
            row++;

            AddSpacer(layout, ref row);

            // Save action row
            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 8, 0, 8)
            };
            btnSaveSettings = new ModernButton
            {
                Text = "💾 Save Configuration",
                StyleType = ButtonStyleType.Primary,
                Width = 220,
                Height = 40
            };
            btnSaveSettings.Click += BtnSaveSettings_Click;
            actionPanel.Controls.Add(btnSaveSettings);

            layout.SetColumnSpan(actionPanel, 4);
            layout.Controls.Add(actionPanel, 0, row);

            mainCard.Controls.Add(layout);
            scrollContent.Controls.Add(mainCard);

            Controls.Add(scrollContent);
            Controls.Add(pnlHeader);

            scrollContent.SendToBack();
            pnlHeader.BringToFront();

            Load += (s, e) => LoadSettingsValues();
        }

        private void AddSectionHeader(TableLayoutPanel layout, string title, ref int row)
        {
            var header = UIHelper.CreateSectionHeader(title);
            header.Margin = new Padding(0, 10, 0, 6);
            layout.SetColumnSpan(header, 4);
            layout.Controls.Add(header, 0, row++);
        }

        private void AddSpacer(TableLayoutPanel layout, ref int row)
        {
            var spacer = new Panel { Height = 10, Dock = DockStyle.Top };
            layout.SetColumnSpan(spacer, 4);
            layout.Controls.Add(spacer, 0, row++);
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeColors.TextPrimary,
                Font = ThemeColors.BodyFont,
                Margin = new Padding(0, 4, 8, 4)
            };
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeColors.BodyFont,
                Margin = new Padding(0, 4, 16, 4)
            };
        }

        public void LoadSettingsValues()
        {
            string modeStr = SettingService.Get("Business.Type", "Mixed");
            if (Enum.TryParse<BusinessType>(modeStr, true, out var mode))
            {
                cmbBusinessType.SelectedItem = mode;
            }
            else
            {
                cmbBusinessType.SelectedItem = BusinessType.Mixed;
            }

            txtCompanyName.Text = SettingService.Get("General.CompanyName", "Easy Installment & Receivables Management");
            txtOwnerName.Text = SettingService.Get("General.OwnerName", "Business Owner");
            txtPhone.Text = SettingService.Get("General.Phone", "+92 300 0000000");
            txtAddress.Text = SettingService.Get("General.Address", "Main Commercial Plaza, City");
            txtCurrency.Text = SettingService.Get("General.Currency", "Rs.");
            txtDateFormat.Text = SettingService.Get("General.DateFormat", "dd/MM/yyyy");

            txtInvoicePrefix.Text = SettingService.Get("Retail.InvoicePrefix", "INV");
            txtProductCodePrefix.Text = SettingService.Get("Retail.ProductCodePrefix", "PRD");
            numGracePeriodDays.Value = SettingService.GetInt("Retail.DefaultGracePeriodDays", 3);
            numDefaultLateFee.Value = decimal.TryParse(SettingService.Get("Retail.DefaultLateFeePercent", "0"), out var lf) ? lf : 0;

            numDefaultDueDay.Value = SettingService.GetInt("Rent.DefaultDueDay", 5);
            numReminderDays.Value = SettingService.GetInt("Rent.ReminderDaysBefore", 3);
            numOverdueDays.Value = SettingService.GetInt("Rent.OverdueDaysAfter", 1);
            numExpiryAlertDays.Value = SettingService.GetInt("Rent.AgreementExpiryReminderDays", 30);

            chkAutoBackupOnExit.Checked = SettingService.GetBool("Backup.AutoBackupOnExit", true);
            numRetentionDays.Value = SettingService.GetInt("Backup.RetentionDays", 30);

            txtReceiptPrefix.Text = SettingService.Get("Receipt.Prefix", "RCP");
            txtReceiptHeader.Text = SettingService.Get("Receipt.HeaderNote", "Official Payment & Account Receipt");
            txtReceiptFooter.Text = SettingService.Get("Receipt.FooterNote", "Thank you for your timely payment. Computer generated receipt.");

            numAutoLockMinutes.Value = SettingService.GetInt("Security.AutoLockMinutes", 15);
        }

        private void BtnSaveSettings_Click(object? sender, EventArgs e)
        {
            var selectedMode = cmbBusinessType.SelectedItem is BusinessType bt ? bt : BusinessType.Mixed;
            SettingService.Set("Business.Type", selectedMode.ToString(), "Business", "Active Operating Business Mode");

            SettingService.Set("General.CompanyName", txtCompanyName.Text.Trim(), "General", "Landlord / Business Name");
            SettingService.Set("General.OwnerName", txtOwnerName.Text.Trim(), "General", "Owner Full Name");
            SettingService.Set("General.Phone", txtPhone.Text.Trim(), "General", "Office Phone");
            SettingService.Set("General.Address", txtAddress.Text.Trim(), "General", "Office Address");
            SettingService.Set("General.Currency", txtCurrency.Text.Trim(), "General", "Currency Symbol");
            SettingService.Set("General.DateFormat", txtDateFormat.Text.Trim(), "General", "Date Format");

            SettingService.Set("Retail.InvoicePrefix", txtInvoicePrefix.Text.Trim(), "Retail", "Sale Invoice Prefix");
            SettingService.Set("Retail.ProductCodePrefix", txtProductCodePrefix.Text.Trim(), "Retail", "Product Code Prefix");
            SettingService.Set("Retail.DefaultGracePeriodDays", numGracePeriodDays.Value.ToString(), "Retail", "Installment Grace Period Days");
            SettingService.Set("Retail.DefaultLateFeePercent", numDefaultLateFee.Value.ToString(), "Retail", "Default Late Fee %");

            SettingService.Set("Rent.DefaultDueDay", numDefaultDueDay.Value.ToString(), "Rent", "Default Due Day");
            SettingService.Set("Rent.ReminderDaysBefore", numReminderDays.Value.ToString(), "Rent", "Reminder Days Before");
            SettingService.Set("Rent.OverdueDaysAfter", numOverdueDays.Value.ToString(), "Rent", "Overdue Threshold Days");
            SettingService.Set("Rent.AgreementExpiryReminderDays", numExpiryAlertDays.Value.ToString(), "Rent", "Lease Expiry Alert Days");

            SettingService.Set("Backup.AutoBackupOnExit", chkAutoBackupOnExit.Checked ? "true" : "false", "Backup", "Auto Backup on Exit");
            SettingService.Set("Backup.RetentionDays", numRetentionDays.Value.ToString(), "Backup", "Backup Retention Days");

            SettingService.Set("Receipt.Prefix", txtReceiptPrefix.Text.Trim(), "Receipt", "Receipt Number Prefix");
            SettingService.Set("Receipt.HeaderNote", txtReceiptHeader.Text.Trim(), "Receipt", "Receipt Header Title");
            SettingService.Set("Receipt.FooterNote", txtReceiptFooter.Text.Trim(), "Receipt", "Receipt Footer Message");

            SettingService.Set("Security.AutoLockMinutes", numAutoLockMinutes.Value.ToString(), "Security", "Auto Lock Minutes");

            SettingService.ReloadCache();
            ModernMessageBox.ShowInfo("All application settings and business rules saved successfully! The new mode and settings are active.", "Settings Saved", this);
        }
    }
}


