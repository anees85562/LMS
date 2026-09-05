using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class FirstRunWizardForm : Form
    {
        private TextBox txtAdminUser = null!;
        private TextBox txtAdminPass = null!;
        private TextBox txtAdminPassConfirm = null!;
        private TextBox txtAdminName = null!;
        private TextBox txtCompanyName = null!;
        private TextBox txtOwnerName = null!;
        private TextBox txtPhone = null!;
        private TextBox txtAddress = null!;
        private TextBox txtCurrency = null!;
        private TextBox txtBackupPath = null!;
        private ModernButton btnFinish = null!;

        public FirstRunWizardForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Landlord & Installment Management System - Initial Setup Wizard";
            Size = new Size(740, 720);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "🚀 Welcome to Easy Receivables Platform",
                "Let's configure your administrator security credentials and business operating profile"
            );

            var scrollContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 14, 20, 14),
                AutoScroll = true
            };

            var pnlCard = UIHelper.CreateCardPanel(new Padding(20, 16, 20, 16));
            pnlCard.Dock = DockStyle.Top;
            pnlCard.AutoSize = true;
            pnlCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 16,
                Padding = new Padding(0)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));

            int row = 0;

            // SECTION 1: Admin Account
            AddSectionHeader(mainPanel, "1. Administrator Security Setup", ref row);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Admin Full Name *"), 0, row);
            txtAdminName = UIHelper.CreateStyledTextBox("System Administrator");
            mainPanel.Controls.Add(txtAdminName, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Admin Username *"), 0, row);
            txtAdminUser = UIHelper.CreateStyledTextBox("admin");
            mainPanel.Controls.Add(txtAdminUser, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Password *"), 0, row);
            txtAdminPass = UIHelper.CreateStyledTextBox("admin123", isPass: true);
            mainPanel.Controls.Add(txtAdminPass, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Confirm Password *"), 0, row);
            txtAdminPassConfirm = UIHelper.CreateStyledTextBox("admin123", isPass: true);
            mainPanel.Controls.Add(txtAdminPassConfirm, 1, row++);

            AddSpacer(mainPanel, ref row);

            // SECTION 2: Landlord / Property Details
            AddSectionHeader(mainPanel, "2. Business & Organization Profile", ref row);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Business / Plaza Name"), 0, row);
            txtCompanyName = UIHelper.CreateStyledTextBox("Easy Receivables & Rentals");
            mainPanel.Controls.Add(txtCompanyName, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Owner Full Name"), 0, row);
            txtOwnerName = UIHelper.CreateStyledTextBox("Business Owner");
            mainPanel.Controls.Add(txtOwnerName, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Contact Phone"), 0, row);
            txtPhone = UIHelper.CreateStyledTextBox("+92 300 1234567");
            mainPanel.Controls.Add(txtPhone, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Office / Plaza Address"), 0, row);
            txtAddress = UIHelper.CreateStyledTextBox("Commercial Market, City");
            mainPanel.Controls.Add(txtAddress, 1, row++);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Currency Symbol"), 0, row);
            txtCurrency = UIHelper.CreateStyledTextBox("Rs.");
            txtCurrency.Width = 120;
            txtCurrency.Dock = DockStyle.Left;
            mainPanel.Controls.Add(txtCurrency, 1, row++);

            AddSpacer(mainPanel, ref row);

            // SECTION 3: Backup Location
            AddSectionHeader(mainPanel, "3. Default Backup Directory", ref row);

            mainPanel.Controls.Add(UIHelper.CreateFieldLabel("Backup Directory"), 0, row);
            var backupFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            string defaultBackup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LandlordManagementSystem", "Backups");
            txtBackupPath = new TextBox { Text = defaultBackup, Width = 340, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            var btnBrowse = new ModernButton
            {
                Text = "Browse...",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 28
            };
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = fbd.SelectedPath;
                }
            };
            backupFlow.Controls.Add(txtBackupPath);
            backupFlow.Controls.Add(btnBrowse);
            mainPanel.Controls.Add(backupFlow, 1, row++);

            pnlCard.Controls.Add(mainPanel);
            scrollContainer.Controls.Add(pnlCard);

            btnFinish = new ModernButton
            {
                Text = "🚀 Complete Setup & Launch",
                StyleType = ButtonStyleType.Primary,
                Width = 250,
                Height = 38
            };
            btnFinish.Click += BtnFinish_Click;

            var pnlDialogFooter = UIHelper.CreateDialogFooter(btnFinish);

            Controls.Add(scrollContainer);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private void AddSectionHeader(TableLayoutPanel layout, string title, ref int row)
        {
            var header = UIHelper.CreateSectionHeader(title);
            header.Margin = new Padding(0, 8, 0, 4);
            layout.SetColumnSpan(header, 2);
            layout.Controls.Add(header, 0, row++);
        }

        private void AddSpacer(TableLayoutPanel layout, ref int row)
        {
            var spacer = new Panel { Height = 10, Dock = DockStyle.Top };
            layout.SetColumnSpan(spacer, 2);
            layout.Controls.Add(spacer, 0, row++);
        }

        private void BtnFinish_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAdminUser.Text))
            {
                ModernMessageBox.ShowWarning("Please enter an administrator username.", "Validation", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAdminPass.Text) || txtAdminPass.Text.Length < 4)
            {
                ModernMessageBox.ShowWarning("Password must be at least 4 characters long.", "Validation", this);
                return;
            }

            if (txtAdminPass.Text != txtAdminPassConfirm.Text)
            {
                ModernMessageBox.ShowWarning("Passwords do not match.", "Validation", this);
                return;
            }

            // Create Administrator User
            var createRes = AuthService.CreateUser(txtAdminUser.Text.Trim(), txtAdminPass.Text, txtAdminName.Text.Trim(), UserRole.Administrator);
            if (!createRes.Success)
            {
                ModernMessageBox.ShowError(createRes.Message, "Error", this);
                return;
            }

            // Save Settings
            SettingService.Set("General.CompanyName", txtCompanyName.Text.Trim(), "General", "Landlord / Business Name");
            SettingService.Set("General.OwnerName", txtOwnerName.Text.Trim(), "General", "Owner Full Name");
            SettingService.Set("General.Phone", txtPhone.Text.Trim(), "General", "Contact Phone");
            SettingService.Set("General.Address", txtAddress.Text.Trim(), "General", "Office Address");
            SettingService.Set("General.Currency", string.IsNullOrWhiteSpace(txtCurrency.Text) ? "Rs." : txtCurrency.Text.Trim(), "General", "Currency Symbol");
            SettingService.Set("Backup.DefaultDirectory", txtBackupPath.Text.Trim(), "Backup", "Default Backup Directory");

            // Seed default landlord profile
            var landlord = PropertyService.GetOrCreateDefaultLandlord();
            landlord.Name = txtOwnerName.Text.Trim();
            landlord.Phone = txtPhone.Text.Trim();
            landlord.Address = txtAddress.Text.Trim();
            PropertyService.SaveLandlord(landlord);

            ModernMessageBox.ShowSuccess("Initial setup completed successfully!\nYou can now log in with your new administrator account.", "Setup Complete", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
