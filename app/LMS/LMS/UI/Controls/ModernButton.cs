using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LMS.UI.Controls
{
    public enum ButtonStyleType
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Warning,
        Info,
        SidebarNav,
        Ghost
    }

    public class ModernButton : Button
    {
        private ButtonStyleType _styleType = ButtonStyleType.Primary;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private bool _isSelected = false;
        private int _borderRadius = 6;

        public ButtonStyleType StyleType
        {
            get => _styleType;
            set { _styleType = value; Invalidate(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Invalidate(); }
        }

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
            UseVisualStyleBackColor = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            Color backColor;
            Color textColor;
            Color borderColor = Color.Transparent;

            switch (_styleType)
            {
                case ButtonStyleType.Primary:
                    backColor = _isPressed ? ThemeColors.PrimaryDark : (_isHovered ? ThemeColors.PrimaryHover : ThemeColors.Primary);
                    textColor = Color.White;
                    break;

                case ButtonStyleType.Success:
                    backColor = _isPressed ? Color.FromArgb(4, 120, 87) : (_isHovered ? ThemeColors.SuccessHover : ThemeColors.Success);
                    textColor = Color.White;
                    break;

                case ButtonStyleType.Danger:
                    backColor = _isPressed ? Color.FromArgb(185, 28, 28) : (_isHovered ? ThemeColors.DangerHover : ThemeColors.Danger);
                    textColor = Color.White;
                    break;

                case ButtonStyleType.Warning:
                    backColor = _isPressed ? Color.FromArgb(180, 83, 9) : (_isHovered ? ThemeColors.WarningHover : ThemeColors.Warning);
                    textColor = Color.White;
                    break;

                case ButtonStyleType.Info:
                    backColor = _isPressed ? Color.FromArgb(3, 105, 161) : (_isHovered ? ThemeColors.InfoHover : ThemeColors.Info);
                    textColor = Color.White;
                    break;

                case ButtonStyleType.Secondary:
                    backColor = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.White);
                    textColor = ThemeColors.TextPrimary;
                    borderColor = ThemeColors.Border;
                    break;

                case ButtonStyleType.Ghost:
                    backColor = _isPressed ? Color.FromArgb(226, 232, 240) : (_isHovered ? Color.FromArgb(241, 245, 249) : Color.Transparent);
                    textColor = ThemeColors.TextSecondary;
                    borderColor = Color.Transparent;
                    break;

                case ButtonStyleType.SidebarNav:
                    if (_isSelected)
                    {
                        backColor = Color.FromArgb(30, 41, 59); // Slate 800
                        textColor = Color.White;
                    }
                    else if (_isHovered)
                    {
                        backColor = ThemeColors.SidebarHover;
                        textColor = Color.White;
                    }
                    else
                    {
                        backColor = ThemeColors.SidebarBg;
                        textColor = ThemeColors.SidebarText;
                    }
                    break;

                default:
                    backColor = ThemeColors.Primary;
                    textColor = Color.White;
                    break;
            }

            if (!Enabled)
            {
                backColor = Color.FromArgb(226, 232, 240);
                textColor = Color.FromArgb(148, 163, 184);
                borderColor = Color.Transparent;
            }

            // Draw button background
            using (var path = GetRoundedRectanglePath(bounds, _borderRadius))
            {
                using (var brush = new SolidBrush(backColor))
                {
                    g.FillPath(brush, path);
                }

                if (borderColor != Color.Transparent)
                {
                    using (var pen = new Pen(borderColor, 1))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Draw active vertical indicator strip for SidebarNav buttons
            if (_styleType == ButtonStyleType.SidebarNav && _isSelected)
            {
                using (var accentBrush = new SolidBrush(ThemeColors.Primary))
                {
                    g.FillRectangle(accentBrush, 0, 4, 4, Height - 8);
                }
            }

            // Draw icon and text
            int contentLeft = (_styleType == ButtonStyleType.SidebarNav) ? 14 : 10;
            if (Image != null)
            {
                int imgY = (Height - Image.Height) / 2;
                g.DrawImage(Image, contentLeft, imgY, Image.Width, Image.Height);
                contentLeft += Image.Width + 8;
            }

            var textFormat = new StringFormat
            {
                Alignment = (_styleType == ButtonStyleType.SidebarNav) ? StringAlignment.Near : StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };

            Rectangle textRect = (_styleType == ButtonStyleType.SidebarNav)
                ? new Rectangle(contentLeft, 0, Width - contentLeft - 8, Height)
                : ClientRectangle;

            using (var textBrush = new SolidBrush(textColor))
            {
                g.DrawString(Text, Font, textBrush, textRect, textFormat);
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(rect.Location, size);

            // top left
            path.AddArc(arc, 180, 90);

            // top right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
