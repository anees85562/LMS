using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkShowPassword = null!;
        private CheckBox chkRemember = null!;
        private ModernButton btnLogin = null!;
        private ModernButton btnExit = null!;
        private Label lblError = null!;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Sign In - Easy Receivables & Rental Management Platform";
            Size = new Size(520, 580);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.SidebarBg;
            Font = new Font("Segoe UI", 9.5f);

            // Brand Header Area
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 16, 24, 8)
            };

            var lblLogo = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI Emoji", 26f),
                ForeColor = Color.White,
                Location = new Point(24, 16),
                Size = new Size(52, 52),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTitle = new Label
            {
                Text = "EASY RECEIVABLES",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(84, 18),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Installment, Retail, BNPL & Rent Management Platform",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = ThemeColors.SidebarMuted,
                Location = new Point(86, 48),
                AutoSize = true
            };

            var lblOffline = new Label
            {
                Text = "🟢 100% Offline Secure Desktop Architecture",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Location = new Point(86, 72),
                AutoSize = true
            };

            headerPanel.Controls.Add(lblLogo);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(lblOffline);
            Controls.Add(headerPanel);

            // Card Panel
            var cardPanel = new Panel
            {
                Location = new Point(36, 115),
                Size = new Size(430, 395),
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            cardPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, cardPanel.Width - 1, cardPanel.Height - 1);
                using var pen = new Pen(ThemeColors.Border, 1);
                using var path = UIHelper.GetRoundedRectanglePath(rect, 8);
                g.DrawPath(pen, path);
            };

            int y = 20;

            var lblCardTitle = new Label
            {
                Text = "Sign In to Your Workspace",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = ThemeColors.TextPrimary,
                Location = new Point(20, y),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblCardTitle);
            y += 36;

            // Username
            var lblUser = new Label
            {
                Text = "Username",
                Font = ThemeColors.LabelBoldFont,
                ForeColor = ThemeColors.TextSecondary,
                Location = new Point(20, y),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblUser);
            y += 22;

            txtUsername = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(390, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            cardPanel.Controls.Add(txtUsername);
            y += 38;

            // Password
            var lblPass = new Label
            {
                Text = "Password",
                Font = ThemeColors.LabelBoldFont,
                ForeColor = ThemeColors.TextSecondary,
                Location = new Point(20, y),
                AutoSize = true
            };
            cardPanel.Controls.Add(lblPass);
            y += 22;

            txtPassword = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(390, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                UseSystemPasswordChar = true
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnLogin_Click(this, EventArgs.Empty);
                }
            };
            cardPanel.Controls.Add(txtPassword);
            y += 36;

            // Show password & Remember me
            chkShowPassword = new CheckBox
            {
                Text = "Show Password",
                Location = new Point(20, y),
                AutoSize = true,
                Font = ThemeColors.SmallFont,
                ForeColor = ThemeColors.TextSecondary,
                Cursor = Cursors.Hand
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            };
            cardPanel.Controls.Add(chkShowPassword);

            chkRemember = new CheckBox
            {
                Text = "Remember Username",
                Location = new Point(255, y),
                AutoSize = true,
                Font = ThemeColors.SmallFont,
                ForeColor = ThemeColors.TextSecondary,
                Cursor = Cursors.Hand
            };
            cardPanel.Controls.Add(chkRemember);
            y += 28;

            // Error Label
            lblError = new Label
            {
                Text = "",
                Font = ThemeColors.SmallBoldFont,
                ForeColor = ThemeColors.Danger,
                Location = new Point(20, y),
                Size = new Size(390, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            cardPanel.Controls.Add(lblError);
            y += 32;

            // Action Buttons
            btnLogin = new ModernButton
            {
                Text = "Sign In",
                StyleType = ButtonStyleType.Primary,
                Location = new Point(20, y),
                Size = new Size(260, 42)
            };
            btnLogin.Click += BtnLogin_Click;
            cardPanel.Controls.Add(btnLogin);

            btnExit = new ModernButton
            {
                Text = "Exit",
                StyleType = ButtonStyleType.Secondary,
                Location = new Point(290, y),
                Size = new Size(120, 42)
            };
            btnExit.Click += (s, e) =>
            {
                Application.Exit();
            };
            cardPanel.Controls.Add(btnExit);

            Controls.Add(cardPanel);

            AcceptButton = btnLogin;
            CancelButton = btnExit;

            Load += LoginForm_Load;
        }

        private void LoginForm_Load(object? sender, EventArgs e)
        {
            // First Run Check: If no Admin account exists, show First Run Setup Wizard
            if (!AuthService.HasAnyAdmin())
            {
                Hide();
                using var wizard = new FirstRunWizardForm();
                if (wizard.ShowDialog() != DialogResult.OK)
                {
                    Application.Exit();
                    return;
                }
                Show();
            }

            string rememberedUser = SettingService.Get("App.RememberedUsername", "");
            if (!string.IsNullOrWhiteSpace(rememberedUser))
            {
                txtUsername.Text = rememberedUser;
                chkRemember.Checked = true;
                txtPassword.Focus();
            }
            else
            {
                txtUsername.Focus();
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;

            var auth = AuthService.Authenticate(user, pass);
            if (!auth.Success)
            {
                lblError.Text = auth.Message;
                txtPassword.SelectAll();
                txtPassword.Focus();
                return;
            }

            if (chkRemember.Checked)
            {
                SettingService.Set("App.RememberedUsername", user, "Security", "Saved username for convenience");
            }
            else
            {
                SettingService.Set("App.RememberedUsername", "", "Security", "Saved username for convenience");
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
