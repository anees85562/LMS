using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LMS.UI.Controls;

namespace LMS.UI
{
    public static class UIHelper
    {
        public static Panel CreatePageHeader(string title, string subtitle, Control? rightSideControl = null)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 10)
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = ThemeColors.TitleFont,
                ForeColor = ThemeColors.TextPrimary,
                Location = new Point(0, 2),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = subtitle,
                Font = ThemeColors.SubtitleFont,
                ForeColor = ThemeColors.TextSecondary,
                Location = new Point(2, 32),
                AutoSize = true
            };

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblSub);

            if (rightSideControl != null)
            {
                rightSideControl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                rightSideControl.Location = new Point(pnl.Width - rightSideControl.Width - 10, 12);
                pnl.Controls.Add(rightSideControl);
            }

            return pnl;
        }

        public static Panel CreateCardPanel(Padding? padding = null)
        {
            var pnl = new Panel
            {
                BackColor = ThemeColors.CardBg,
                Padding = padding ?? new Padding(16)
            };
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using var pen = new Pen(ThemeColors.Border, 1);
                using var path = GetRoundedRectanglePath(rect, 6);
                g.DrawPath(pen, path);
            };
            return pnl;
        }

        public static Panel CreateFilterBar(int height = 56)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = height,
                BackColor = ThemeColors.CardBg,
                Padding = new Padding(12, 10, 12, 10)
            };
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using var pen = new Pen(ThemeColors.Border, 1);
                using var path = GetRoundedRectanglePath(rect, 6);
                g.DrawPath(pen, path);
            };
            return pnl;
        }

        public static Panel CreateSectionHeader(string title)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 6)
            };

            var lbl = new Label
            {
                Text = title,
                Font = ThemeColors.SectionFont,
                ForeColor = ThemeColors.TextPrimary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        public static Panel CreateDialogHeader(string title, string subtitle, string icon = "⚡")
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = ThemeColors.SidebarBg,
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 16f),
                ForeColor = Color.White,
                Location = new Point(14, 12),
                Size = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(54, 12),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ThemeColors.SidebarMuted,
                Location = new Point(56, 36),
                AutoSize = true
            };

            pnl.Controls.Add(lblIcon);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(lblSub);
            return pnl;
        }

        public static Panel CreateDialogFooter(params Button[] buttons)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                BackColor = ThemeColors.CanvasBg,
                Padding = new Padding(16, 10, 16, 10)
            };

            pnl.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, pnl.Width, 0);
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0)
            };

            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 0);
                    flow.Controls.Add(buttons[i]);
                }
            }

            pnl.Controls.Add(flow);
            return pnl;
        }

        public static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeColors.TextPrimary,
                Font = ThemeColors.LabelBoldFont
            };
        }

        public static TextBox CreateStyledTextBox(string defaultVal = "", bool isPass = false)
        {
            return new TextBox
            {
                Text = defaultVal,
                Dock = DockStyle.Fill,
                Height = 28,
                UseSystemPasswordChar = isPass,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeColors.BodyFont
            };
        }

        public static NumericUpDown CreateStyledNumeric(decimal min, decimal max, int decimals = 2, decimal initial = 0)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Value = Math.Clamp(initial, min, max),
                Dock = DockStyle.Fill,
                Height = 28,
                Font = ThemeColors.BodyFont
            };
        }

        public static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
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
