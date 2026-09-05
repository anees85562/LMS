using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class UserEditForm : Form
    {
        private User? _user;
        private TextBox txtUsername = null!;
        private TextBox txtFullName = null!;
        private ComboBox cmbRole = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkActive = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        public UserEditForm(User? user = null)
        {
            _user = user;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = _user == null ? "Add New User" : "Edit User Account";
            Size = new Size(540, 470);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _user == null ? "👤 Add New User Account" : "👤 Edit User Account Details",
                "Configure username, system role permissions, credentials, and active status"
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

            // Username
            mainPanel.Controls.Add(CreateFieldLabel("Username *"), 0, 0);
            txtUsername = new TextBox
            {
                Text = _user?.Username ?? "",
                Dock = DockStyle.Fill,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeColors.BodyFont,
                Enabled = _user == null
            };
            mainPanel.Controls.Add(txtUsername, 1, 0);

            // Full Name
            mainPanel.Controls.Add(CreateFieldLabel("Full Name *"), 0, 1);
            txtFullName = new TextBox { Text = _user?.FullName ?? "", Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtFullName, 1, 1);

            // Role
            mainPanel.Controls.Add(CreateFieldLabel("User Role *"), 0, 2);
            cmbRole = new ComboBox { Dock = DockStyle.Fill, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new object[] { UserRole.Administrator, UserRole.Operator, UserRole.Viewer });
            cmbRole.SelectedItem = _user?.Role ?? UserRole.Operator;
            mainPanel.Controls.Add(cmbRole, 1, 2);

            // Password
            mainPanel.Controls.Add(CreateFieldLabel(_user == null ? "Password *" : "Reset Password"), 0, 3);
            txtPassword = new TextBox { Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(txtPassword, 1, 3);

            // Active
            mainPanel.Controls.Add(CreateFieldLabel("Status"), 0, 4);
            chkActive = new CheckBox
            {
                Text = "Account is Active and Allowed to Login",
                Dock = DockStyle.Fill,
                Checked = _user?.IsActive ?? true,
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.TextPrimary
            };
            mainPanel.Controls.Add(chkActive, 1, 4);

            pnlCard.Controls.Add(mainPanel);

            btnSave = new ModernButton
            {
                Text = "✓ Save Account",
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

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ModernMessageBox.ShowWarning("Please enter full name.", "Validation", this);
                return;
            }

            var role = (UserRole)(cmbRole.SelectedItem ?? UserRole.Operator);

            if (_user == null)
            {
                // Create user
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || txtUsername.Text.Trim().Length < 3)
                {
                    ModernMessageBox.ShowWarning("Username must be at least 3 characters long.", "Validation", this);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text.Length < 4)
                {
                    ModernMessageBox.ShowWarning("Password must be at least 4 characters long.", "Validation", this);
                    return;
                }

                var res = AuthService.CreateUser(txtUsername.Text.Trim(), txtPassword.Text, txtFullName.Text.Trim(), role);
                if (!res.Success)
                {
                    ModernMessageBox.ShowError(res.Message, "Error", this);
                    return;
                }
            }
            else
            {
                // Update user
                var res = AuthService.UpdateUser(_user.Id, txtFullName.Text.Trim(), role, chkActive.Checked);
                if (!res.Success)
                {
                    ModernMessageBox.ShowError(res.Message, "Error", this);
                    return;
                }

                // If password provided, reset it
                if (!string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    var passRes = AuthService.ResetPassword(_user.Id, txtPassword.Text);
                    if (!passRes.Success)
                    {
                        ModernMessageBox.ShowWarning(passRes.Message, "Warning", this);
                    }
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
