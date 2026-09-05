using System.Drawing;

namespace LMS.UI
{
    public static class ThemeColors
    {
        // Primary & Brand Colors
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);         // Royal Blue (#2563EB)
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);     // Darker Blue (#1D4ED8)
        public static readonly Color PrimaryHover = Color.FromArgb(30, 64, 175);    // Deep Blue (#1E40AF)
        public static readonly Color PrimaryLight = Color.FromArgb(239, 246, 255);  // Ice Blue (#EFF6FF)
        public static readonly Color PrimaryBorder = Color.FromArgb(191, 219, 254); // Light Blue Border (#BFDBFE)

        // Dark Sidebar Theme
        public static readonly Color SidebarBg = Color.FromArgb(15, 23, 42);        // Slate 900 (#0F172A)
        public static readonly Color SidebarHeader = Color.FromArgb(30, 41, 59);    // Slate 800 (#1E293B)
        public static readonly Color SidebarHover = Color.FromArgb(51, 65, 85);     // Slate 700 (#334155)
        public static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);   // Royal Blue (#2563EB)
        public static readonly Color SidebarText = Color.FromArgb(226, 232, 240);   // Slate 200 (#E2E8F0)
        public static readonly Color SidebarMuted = Color.FromArgb(148, 163, 184);  // Slate 400 (#94A3B8)
        public static readonly Color SidebarAccent = Color.FromArgb(59, 130, 246);  // Blue 500

        // Canvas & Surface
        public static readonly Color CanvasBg = Color.FromArgb(248, 250, 252);      // Slate 50 (#F8FAFC)
        public static readonly Color CardBg = Color.FromArgb(255, 255, 255);        // Pure White (#FFFFFF)
        public static readonly Color Border = Color.FromArgb(226, 232, 240);        // Slate 200 (#E2E8F0)
        public static readonly Color BorderDark = Color.FromArgb(203, 213, 225);    // Slate 300 (#CBD5E1)
        public static readonly Color HeaderBg = Color.FromArgb(241, 245, 249);      // Slate 100 (#F1F5F9)
        public static readonly Color RowHover = Color.FromArgb(241, 245, 249);      // Slate 100

        // Typography Colors
        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);      // Slate 900 (#0F172A)
        public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);   // Slate 600 (#475569)
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);     // Slate 400 (#94A3B8)

        // Functional Status Colors
        public static readonly Color Success = Color.FromArgb(16, 185, 129);        // Emerald 500 (#10B981)
        public static readonly Color SuccessHover = Color.FromArgb(5, 150, 105);   // Emerald 600 (#059669)
        public static readonly Color SuccessLight = Color.FromArgb(236, 253, 245); // Emerald 50 (#ECFDF5)
        public static readonly Color SuccessBorder = Color.FromArgb(167, 243, 208);// Emerald 200
        public static readonly Color SuccessText = Color.FromArgb(6, 95, 70);       // Emerald 800 (#065F46)

        public static readonly Color Warning = Color.FromArgb(245, 158, 11);       // Amber 500 (#F59E0B)
        public static readonly Color WarningHover = Color.FromArgb(217, 119, 6);   // Amber 600 (#D97706)
        public static readonly Color WarningLight = Color.FromArgb(254, 243, 199); // Amber 50 (#FEF3C7)
        public static readonly Color WarningBorder = Color.FromArgb(253, 230, 138);// Amber 200
        public static readonly Color WarningText = Color.FromArgb(146, 64, 14);     // Amber 800 (#92400E)

        public static readonly Color Danger = Color.FromArgb(239, 68, 68);          // Red 500 (#EF4444)
        public static readonly Color DangerHover = Color.FromArgb(220, 38, 38);     // Red 600 (#DC2626)
        public static readonly Color DangerLight = Color.FromArgb(254, 242, 242);  // Red 50 (#FEF2F2)
        public static readonly Color DangerBorder = Color.FromArgb(254, 202, 202); // Red 200
        public static readonly Color DangerText = Color.FromArgb(153, 27, 27);      // Red 800 (#991B1B)

        public static readonly Color Info = Color.FromArgb(14, 165, 233);           // Sky 500 (#0EA5E9)
        public static readonly Color InfoHover = Color.FromArgb(2, 132, 199);       // Sky 600
        public static readonly Color InfoLight = Color.FromArgb(240, 249, 255);    // Sky 50 (#F0F9FF)
        public static readonly Color InfoBorder = Color.FromArgb(186, 230, 253);   // Sky 200
        public static readonly Color InfoText = Color.FromArgb(7, 89, 133);         // Sky 800 (#075985)

        public static readonly Color AccentPurple = Color.FromArgb(139, 92, 246);  // Purple 500
        public static readonly Color AccentPurpleLight = Color.FromArgb(245, 243, 255);

        // Standard Typography Tokens (Segoe UI)
        public static readonly Font TitleFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        public static readonly Font SubtitleFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font SectionFont = new Font("Segoe UI", 10.8f, FontStyle.Bold);
        public static readonly Font SubHeadingFont = new Font("Segoe UI", 9.8f, FontStyle.Bold);
        public static readonly Font SubHeadingRegularFont = new Font("Segoe UI", 9.8f, FontStyle.Regular);
        public static readonly Font LabelBoldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.2f, FontStyle.Regular);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9.2f, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.2f, FontStyle.Regular);
        public static readonly Font SmallBoldFont = new Font("Segoe UI", 8.2f, FontStyle.Bold);
        public static readonly Font MetricFont = new Font("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font MetricLargeFont = new Font("Segoe UI", 17f, FontStyle.Bold);
    }
}
