using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LMS.UI.Controls
{
    public class StatCardControl : UserControl
    {
        private string _title = "Metric Title";
        private string _value = "0";
        private string _subtitle = "";
        private string _iconSymbol = "📊";
        private Color _accentColor = ThemeColors.Primary;

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; Invalidate(); }
        }

        public string IconSymbol
        {
            get => _iconSymbol;
            set { _iconSymbol = value; Invalidate(); }
        }

        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        public StatCardControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
            Size = new Size(240, 110);
            Font = new Font("Segoe UI", 9f);
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

            // Draw Card Background with rounded corners
            using (var path = GetRoundedRectanglePath(bounds, 8))
            {
                using (var bgBrush = new SolidBrush(ThemeColors.CardBg))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(ThemeColors.Border, 1))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw Top Accent Line
            using (var accentBrush = new SolidBrush(_accentColor))
            {
                g.FillRectangle(accentBrush, 8, 0, Width - 16, 3);
            }

            // Draw Icon Circle
            int circleSize = 42;
            int circleX = Width - circleSize - 14;
            int circleY = 16;
            var circleRect = new Rectangle(circleX, circleY, circleSize, circleSize);

            using (var circleBg = new SolidBrush(Color.FromArgb(28, _accentColor.R, _accentColor.G, _accentColor.B)))
            {
                g.FillEllipse(circleBg, circleRect);
            }

            using (var iconFont = new Font("Segoe UI Emoji", 14f))
            using (var iconBrush = new SolidBrush(_accentColor))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_iconSymbol, iconFont, iconBrush, circleRect, sf);
            }

            // Draw Title
            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(ThemeColors.TextSecondary))
            {
                g.DrawString(_title, titleFont, titleBrush, 14, 14);
            }

            // Draw Value
            using (var valFont = new Font("Segoe UI", 16f, FontStyle.Bold))
            using (var valBrush = new SolidBrush(ThemeColors.TextPrimary))
            {
                g.DrawString(_value, valFont, valBrush, 14, 34);
            }

            // Draw Subtitle / Badge
            if (!string.IsNullOrWhiteSpace(_subtitle))
            {
                using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var subBrush = new SolidBrush(_accentColor))
                {
                    g.DrawString(_subtitle, subFont, subBrush, 14, 78);
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
