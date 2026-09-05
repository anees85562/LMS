using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LMS.UI.Controls
{
    public class ModernDataGridView : DataGridView
    {
        public ModernDataGridView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, false);
            DoubleBuffered = true;

            BackgroundColor = Color.White;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridColor = ThemeColors.Border;

            EnableHeadersVisualStyles = false;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            ColumnHeadersDefaultCellStyle.BackColor = ThemeColors.HeaderBg;
            ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(51, 65, 85);
            ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
            ColumnHeadersHeight = 40;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            RowHeadersVisible = false;
            RowTemplate.Height = 38;

            DefaultCellStyle.BackColor = Color.White;
            DefaultCellStyle.ForeColor = ThemeColors.TextPrimary;
            DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);
            DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);

            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            AlternatingRowsDefaultCellStyle.ForeColor = ThemeColors.TextPrimary;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255);
            AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);

            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            AutoGenerateColumns = true;
        }

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);

            // Automatically format numeric and currency columns to align right
            foreach (DataGridViewColumn col in Columns)
            {
                string header = (col.HeaderText ?? col.Name).ToLower();
                if (header.Contains("amount") || header.Contains("price") || header.Contains("balance") ||
                    header.Contains("debit") || header.Contains("credit") || header.Contains("cost") ||
                    header.Contains("rent") || header.Contains("arrear") || header.Contains("demanded") ||
                    header.Contains("valuation") || header.Contains("paid") || header.Contains("total") ||
                    header.Contains("remaining") || header.Contains("overdue") || header.Contains("rate") ||
                    header.Contains("stock") || header.Contains("qty"))
                {
                    if (!header.Contains("status") && !header.Contains("date") && !header.Contains("code") && !header.Contains("type"))
                    {
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                }
                else if (header.Contains("status") || header.Contains("rating") || header.Contains("bucket"))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string header = Columns[e.ColumnIndex].HeaderText?.ToLower() ?? "";
                var val = e.Value?.ToString() ?? "";

                // Paint status chip/badge if this is a Status, Rating, or Bucket column
                if (header.Contains("status") || header.Contains("rating") || header.Contains("bucket"))
                {
                    e.PaintBackground(e.CellBounds, true);

                    Color chipBg;
                    Color chipText;
                    string valLower = val.Trim().ToLower();

                    switch (valLower)
                    {
                        case "paid":
                        case "active":
                        case "verified":
                        case "completed":
                        case "settled":
                        case "in stock":
                        case "good":
                        case "1-7 days":
                            chipBg = ThemeColors.SuccessLight;
                            chipText = ThemeColors.SuccessText;
                            break;

                        case "partial":
                        case "partiallypaid":
                        case "pending":
                        case "fair":
                        case "8-30 days":
                        case "undermaintenance":
                            chipBg = ThemeColors.WarningLight;
                            chipText = ThemeColors.WarningText;
                            break;

                        case "overdue":
                        case "terminated":
                        case "evicted":
                        case "blacklisted":
                        case "out of stock":
                        case "low stock":
                        case "risky":
                        case "defaulter":
                        case "disabled":
                        case "31-60 days":
                        case "60+ days":
                            chipBg = ThemeColors.DangerLight;
                            chipText = ThemeColors.DangerText;
                            break;

                        case "vacant":
                        case "advance":
                        case "unverified":
                            chipBg = ThemeColors.InfoLight;
                            chipText = ThemeColors.InfoText;
                            break;

                        case "occupied":
                            chipBg = ThemeColors.AccentPurpleLight;
                            chipText = Color.FromArgb(109, 40, 217);
                            break;

                        default:
                            chipBg = Color.FromArgb(241, 245, 249);
                            chipText = Color.FromArgb(71, 85, 105);
                            break;
                    }

                    var g = e.Graphics;
                    if (g != null)
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                        var size = g.MeasureString(val, font);

                        int chipWidth = Math.Max(70, (int)size.Width + 18);
                        int chipHeight = 24;
                        int chipX = e.CellBounds.Left + (e.CellBounds.Width - chipWidth) / 2;
                        int chipY = e.CellBounds.Top + (e.CellBounds.Height - chipHeight) / 2;

                        var chipRect = new Rectangle(chipX, chipY, chipWidth, chipHeight);
                        using (var path = GetRoundedRectanglePath(chipRect, 4))
                        {
                            using (var bgBrush = new SolidBrush(chipBg))
                            {
                                g.FillPath(bgBrush, path);
                            }
                        }

                        using (var textBrush = new SolidBrush(chipText))
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(val, font, textBrush, chipRect, sf);
                        }
                    }

                    e.Handled = true;
                    return;
                }
            }

            base.OnCellPainting(e);
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
