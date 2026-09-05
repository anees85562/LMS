using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LMS.UI.Controls
{
    public class AlertBannerControl : UserControl
    {
        private string _title = "Alert Title";
        private string _message = "Alert detailed message.";
        private string _severity = "Warning";
        private string _actionText = "View";
        private Action? _onActionClick;

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string MessageText
        {
            get => _message;
            set { _message = value; Invalidate(); }
        }

        public string Severity
        {
            get => _severity;
            set { _severity = value; Invalidate(); }
        }

        public string ActionText
        {
            get => _actionText;
            set { _actionText = value; Invalidate(); }
        }

        public Action? OnActionClick
        {
            get => _onActionClick;
            set => _onActionClick = value;
        }

        private Rectangle _actionButtonRect;
        private bool _isActionHovered = false;

        public AlertBannerControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            Size = new Size(500, 58);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
            Cursor = Cursors.Default;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hovered = _actionButtonRect.Contains(e.Location);
            if (hovered != _isActionHovered)
            {
                _isActionHovered = hovered;
                Cursor = hovered ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isActionHovered)
            {
                _isActionHovered = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_actionButtonRect.Contains(e.Location))
            {
                _onActionClick?.Invoke();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            Color bg;
            Color border;
            Color textMain;
            Color textSub;
            Color accent;
            string icon;

            switch (_severity.ToLower())
            {
                case "danger":
                    bg = ThemeColors.DangerLight;
                    border = ThemeColors.DangerBorder;
                    accent = ThemeColors.Danger;
                    textMain = ThemeColors.DangerText;
                    textSub = Color.FromArgb(185, 28, 28);
                    icon = "⚠️";
                    break;

                case "warning":
                    bg = ThemeColors.WarningLight;
                    border = ThemeColors.WarningBorder;
                    accent = ThemeColors.Warning;
                    textMain = ThemeColors.WarningText;
                    textSub = Color.FromArgb(180, 83, 9);
                    icon = "🔔";
                    break;

                case "success":
                    bg = ThemeColors.SuccessLight;
                    border = ThemeColors.SuccessBorder;
                    accent = ThemeColors.Success;
                    textMain = ThemeColors.SuccessText;
                    textSub = ThemeColors.SuccessText;
                    icon = "✅";
                    break;

                case "info":
                default:
                    bg = ThemeColors.InfoLight;
                    border = ThemeColors.InfoBorder;
                    accent = ThemeColors.Info;
                    textMain = ThemeColors.InfoText;
                    textSub = Color.FromArgb(3, 105, 161);
                    icon = "ℹ️";
                    break;
            }

            // Draw Box with rounded corners
            using (var path = GetRoundedRectanglePath(bounds, 6))
            {
                using (var bgBrush = new SolidBrush(bg))
                {
                    g.FillPath(bgBrush, path);
                }
                using (var borderPen = new Pen(border, 1))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw Left Color Strip
            using (var stripBrush = new SolidBrush(accent))
            {
                g.FillRectangle(stripBrush, 0, 4, 4, Height - 8);
            }

            // Draw Icon
            using (var iconFont = new Font("Segoe UI Emoji", 12f))
            using (var iconBrush = new SolidBrush(accent))
            {
                g.DrawString(icon, iconFont, iconBrush, 12, 16);
            }

            // Draw Title & Message
            using (var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(textMain))
            {
                g.DrawString(_title, titleFont, titleBrush, 40, 10);
            }

            int btnWidth = 85;
            int btnHeight = 28;
            int btnX = Width - btnWidth - 14;
            int btnY = (Height - btnHeight) / 2;
            _actionButtonRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

            using (var msgFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (var msgBrush = new SolidBrush(textSub))
            {
                int maxMsgWidth = Width - 40 - btnWidth - 30;
                var msgRect = new Rectangle(40, 30, Math.Max(20, maxMsgWidth), 22);
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_message, msgFont, msgBrush, msgRect, sf);
            }

            // Draw Action Button if text provided
            if (!string.IsNullOrWhiteSpace(_actionText) && _onActionClick != null)
            {
                using (var btnPath = GetRoundedRectanglePath(_actionButtonRect, 4))
                {
                    Color btnBg = _isActionHovered ? Color.FromArgb(200, accent.R, accent.G, accent.B) : accent;
                    using (var btnBrush = new SolidBrush(btnBg))
                    {
                        g.FillPath(btnBrush, btnPath);
                    }
                }

                using (var btnTextFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var btnTextBrush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(_actionText, btnTextFont, btnTextBrush, _actionButtonRect, sf);
                }
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(rect.Location, size);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
