using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LMS.UI.Controls
{
    public enum MessageBoxIconType
    {
        Information,
        Success,
        Warning,
        Error,
        Question
    }

    public enum MessageBoxButtonType
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
    }

    public class ModernMessageBox : Form
    {
        private MessageBoxIconType _iconType;
        private MessageBoxButtonType _buttonType;
        private Label _lblTitle = null!;
        private Label _lblMessage = null!;
        private Label _lblIcon = null!;

        private ModernMessageBox(string message, string title, MessageBoxButtonType buttonType, MessageBoxIconType iconType)
        {
            _iconType = iconType;
            _buttonType = buttonType;

            InitializeCustomDialog(title, message);
        }

        private void InitializeCustomDialog(string title, string message)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;
            Size = new Size(500, 240);
            MinimumSize = new Size(460, 220);

            // Header panel
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = ThemeColors.SidebarBg,
                Padding = new Padding(16, 12, 16, 12)
            };

            string iconEmoji;
            Color iconColor;

            switch (_iconType)
            {
                case MessageBoxIconType.Success:
                    iconEmoji = "✅";
                    iconColor = ThemeColors.Success;
                    break;
                case MessageBoxIconType.Warning:
                    iconEmoji = "⚠️";
                    iconColor = ThemeColors.Warning;
                    break;
                case MessageBoxIconType.Error:
                    iconEmoji = "❌";
                    iconColor = ThemeColors.Danger;
                    break;
                case MessageBoxIconType.Question:
                    iconEmoji = "❓";
                    iconColor = ThemeColors.Info;
                    break;
                default:
                    iconEmoji = "ℹ️";
                    iconColor = ThemeColors.Info;
                    break;
            }

            _lblIcon = new Label
            {
                Text = iconEmoji,
                Font = new Font("Segoe UI Emoji", 15f),
                Location = new Point(14, 12),
                Size = new Size(32, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(52, 15),
                AutoSize = true
            };

            header.Controls.Add(_lblIcon);
            header.Controls.Add(_lblTitle);
            Controls.Add(header);

            // Message Body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 16),
                BackColor = Color.White
            };

            _lblMessage = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9.8f),
                ForeColor = ThemeColors.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };
            pnlBody.Controls.Add(_lblMessage);
            Controls.Add(pnlBody);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = ThemeColors.CanvasBg,
                Padding = new Padding(16, 10, 16, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0)
            };

            if (_buttonType == MessageBoxButtonType.OK)
            {
                var btnOk = new ModernButton
                {
                    Text = "OK",
                    StyleType = ButtonStyleType.Primary,
                    Size = new Size(100, 36)
                };
                btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
                flow.Controls.Add(btnOk);
                AcceptButton = btnOk;
            }
            else if (_buttonType == MessageBoxButtonType.OKCancel)
            {
                var btnCancel = new ModernButton
                {
                    Text = "Cancel",
                    StyleType = ButtonStyleType.Secondary,
                    Size = new Size(100, 36),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                var btnOk = new ModernButton
                {
                    Text = "OK",
                    StyleType = ButtonStyleType.Primary,
                    Size = new Size(100, 36)
                };
                btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

                flow.Controls.Add(btnCancel);
                flow.Controls.Add(btnOk);
                AcceptButton = btnOk;
                CancelButton = btnCancel;
            }
            else if (_buttonType == MessageBoxButtonType.YesNo)
            {
                var btnNo = new ModernButton
                {
                    Text = "No",
                    StyleType = ButtonStyleType.Secondary,
                    Size = new Size(95, 36),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnNo.Click += (s, e) => { DialogResult = DialogResult.No; Close(); };

                var btnYes = new ModernButton
                {
                    Text = "Yes",
                    StyleType = _iconType == MessageBoxIconType.Warning || _iconType == MessageBoxIconType.Error ? ButtonStyleType.Danger : ButtonStyleType.Primary,
                    Size = new Size(95, 36)
                };
                btnYes.Click += (s, e) => { DialogResult = DialogResult.Yes; Close(); };

                flow.Controls.Add(btnNo);
                flow.Controls.Add(btnYes);
                AcceptButton = btnYes;
                CancelButton = btnNo;
            }
            else if (_buttonType == MessageBoxButtonType.YesNoCancel)
            {
                var btnCancel = new ModernButton
                {
                    Text = "Cancel",
                    StyleType = ButtonStyleType.Secondary,
                    Size = new Size(90, 36),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                var btnNo = new ModernButton
                {
                    Text = "No",
                    StyleType = ButtonStyleType.Secondary,
                    Size = new Size(90, 36),
                    Margin = new Padding(8, 0, 0, 0)
                };
                btnNo.Click += (s, e) => { DialogResult = DialogResult.No; Close(); };

                var btnYes = new ModernButton
                {
                    Text = "Yes",
                    StyleType = ButtonStyleType.Primary,
                    Size = new Size(90, 36)
                };
                btnYes.Click += (s, e) => { DialogResult = DialogResult.Yes; Close(); };

                flow.Controls.Add(btnCancel);
                flow.Controls.Add(btnNo);
                flow.Controls.Add(btnYes);
                AcceptButton = btnYes;
                CancelButton = btnCancel;
            }

            pnlFooter.Controls.Add(flow);
            Controls.Add(pnlFooter);
        }

        public static DialogResult Show(string message, string title = "Information", MessageBoxButtonType buttons = MessageBoxButtonType.OK, MessageBoxIconType icon = MessageBoxIconType.Information, IWin32Window? owner = null)
        {
            using var dlg = new ModernMessageBox(message, title, buttons, icon);
            if (owner != null) return dlg.ShowDialog(owner);
            return dlg.ShowDialog();
        }

        public static DialogResult ShowInfo(string message, string title = "Information", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.OK, MessageBoxIconType.Information, owner);
        }

        public static DialogResult ShowSuccess(string message, string title = "Success", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.OK, MessageBoxIconType.Success, owner);
        }

        public static DialogResult ShowWarning(string message, string title = "Warning", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.OK, MessageBoxIconType.Warning, owner);
        }

        public static DialogResult ShowError(string message, string title = "Error", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.OK, MessageBoxIconType.Error, owner);
        }

        public static bool Confirm(string message, string title = "Confirm Action", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.YesNo, MessageBoxIconType.Question, owner) == DialogResult.Yes;
        }

        public static bool ShowConfirm(string message, string title = "Confirm Action", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.YesNo, MessageBoxIconType.Question, owner) == DialogResult.Yes;
        }

        public static bool ConfirmDanger(string message, string title = "Confirm Critical Action", IWin32Window? owner = null)
        {
            return Show(message, title, MessageBoxButtonType.YesNo, MessageBoxIconType.Warning, owner) == DialogResult.Yes;
        }
    }
}
