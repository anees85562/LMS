using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Forms.Dialogs;
using LMS.Forms.Views;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms
{
    public class MainForm : Form
    {
        private Panel pnlSidebar = null!;
        private Panel pnlHeader = null!;
        private Panel pnlContent = null!;
        private Label lblPageTitle = null!;
        private TextBox txtGlobalSearch = null!;
        private Label lblUserBadge = null!;
        private System.Windows.Forms.Timer? _inactivityTimer;
        private DateTime _lastActivity = DateTime.Now;

        private Dictionary<string, ModernButton> _navButtons = new();
        private Dictionary<string, UserControl> _viewCache = new();
        private string _currentViewKey = "";

        // Services
        private readonly AuditService _auditService = new AuditService();
        private readonly SettingService _settingService = new SettingService();
        private readonly TerminologyService _terminology;
        private readonly ProductService _productService;
        private readonly InventoryService _inventoryService;
        private readonly InstallmentSaleService _saleService;
        private readonly ReceivablesService _receivablesService = new ReceivablesService();

        public MainForm()
        {
            _terminology = new TerminologyService(_settingService);
            _inventoryService = new InventoryService(_auditService);
            _productService = new ProductService(_auditService);
            _saleService = new InstallmentSaleService(_inventoryService, _auditService, _settingService);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Easy Receivables - Universal Installment, BNPL, Retail & Property Management Platform";
            Size = new Size(1366, 768);
            MinimumSize = new Size(1100, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);

            // 1. LEFT SIDEBAR
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 245,
                BackColor = ThemeColors.SidebarBg,
                Padding = new Padding(0)
            };

            // Sidebar Header
            var pnlSideHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = ThemeColors.SidebarHeader
            };

            var lblLogo = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI Emoji", 20f),
                ForeColor = Color.White,
                Location = new Point(14, 15),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblAppName = new Label
            {
                Text = "EASY RECEIVABLES",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(58, 18),
                AutoSize = true
            };

            var lblAppSub = new Label
            {
                Text = "Installments, BNPL & Rent",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = ThemeColors.SidebarMuted,
                Location = new Point(60, 42),
                AutoSize = true
            };

            pnlSideHeader.Controls.Add(lblLogo);
            pnlSideHeader.Controls.Add(lblAppName);
            pnlSideHeader.Controls.Add(lblAppSub);
            pnlSidebar.Controls.Add(pnlSideHeader);

            // Navigation Button Container
            var pnlNavList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ThemeColors.SidebarBg,
                Padding = new Padding(8, 4, 8, 12)
            };

            AddNavSection(pnlNavList, "MAIN MENU");
            AddNavButton(pnlNavList, "Dashboard", "📊  Dashboard");

            AddNavSection(pnlNavList, "RECEIVABLES & SALES");
            AddNavButton(pnlNavList, "Sales", "🛒  Installment & Retail Sales");
            AddNavButton(pnlNavList, "Products", "📦  Products & Inventory");
            AddNavButton(pnlNavList, "Defaulters", "⚠️  Defaulters & Aging");
            AddNavButton(pnlNavList, "Profiles", "👤  Credit Profiles");

            AddNavSection(pnlNavList, "CUSTOMERS & RENT");
            AddNavButton(pnlNavList, "Tenants", "👥  Customers / Tenants");
            AddNavButton(pnlNavList, "Properties", "🏢  Properties & Units");
            AddNavButton(pnlNavList, "Register", "📖  Universal Register");
            AddNavButton(pnlNavList, "Billing", "💵  Rent Processing");
            AddNavButton(pnlNavList, "Ledger", "📜  Universal Ledger");

            AddNavSection(pnlNavList, "REPORTS & SYSTEM");
            AddNavButton(pnlNavList, "Reports", "📈  Reports & Analytics");
            AddNavButton(pnlNavList, "Backup", "💾  Backup & Restore");
            AddNavButton(pnlNavList, "ImportExport", "📥  CSV Migration");

            if (AuthService.IsAdmin)
            {
                AddNavButton(pnlNavList, "Users", "🔐  Users & Security");
            }

            AddNavButton(pnlNavList, "Settings", "⚙️  System Settings");

            pnlSidebar.Controls.Add(pnlNavList);

            // Sidebar Bottom: Offline Mode indicator
            var pnlSideBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = ThemeColors.SidebarHeader
            };
            var lblOfflineStatus = new Label
            {
                Text = "🟢 100% Offline SQLite",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Location = new Point(14, 14),
                AutoSize = true
            };
            pnlSideBottom.Controls.Add(lblOfflineStatus);
            pnlSidebar.Controls.Add(pnlSideBottom);

            // 2. TOP HEADER
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0)
            };
            pnlHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border);
                e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
            };

            lblPageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = ThemeColors.TextPrimary,
                Location = new Point(20, 18),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblPageTitle);

            // Global Quick Search
            txtGlobalSearch = new TextBox
            {
                Location = new Point(340, 18),
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "🔍 Global Search (Customer, Product, Barcode, Inv)..."
            };
            txtGlobalSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtGlobalSearch.Text))
                {
                    PerformGlobalSearch(txtGlobalSearch.Text.Trim());
                }
            };
            pnlHeader.Controls.Add(txtGlobalSearch);

            // User Badge & Logout
            string uName = AuthService.CurrentUser?.FullName ?? "Administrator";
            string role = AuthService.CurrentUser?.Role.ToString() ?? "Admin";

            lblUserBadge = new Label
            {
                Text = $"👤 {uName} ({role})",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ThemeColors.TextPrimary,
                Location = new Point(pnlHeader.Width - 360, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblUserBadge);

            var btnLogout = new ModernButton
            {
                Text = "Logout",
                StyleType = ButtonStyleType.Secondary,
                Location = new Point(pnlHeader.Width - 110, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(90, 34)
            };
            btnLogout.Click += (s, e) =>
            {
                if (ModernMessageBox.Confirm("Are you sure you want to log out of your session?", "Logout Confirmation", this))
                {
                    AuthService.Logout();
                    Hide();
                    using var login = new LoginForm();
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        lblUserBadge.Text = $"👤 {AuthService.CurrentUser?.FullName} ({AuthService.CurrentUser?.Role})";
                        NavigateTo("Dashboard");
                        Show();
                    }
                    else
                    {
                        Application.Exit();
                    }
                }
            };
            pnlHeader.Controls.Add(btnLogout);

            // 3. MAIN CONTENT HOST
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.CanvasBg
            };

            // Stacking order: Content added first, then Header, then Sidebar
            Controls.Add(pnlContent);
            Controls.Add(pnlHeader);
            Controls.Add(pnlSidebar);

            // Inactivity timer
            int autoLockMins = SettingService.GetInt("Security.AutoLockMinutes", 15);
            if (autoLockMins > 0)
            {
                _inactivityTimer = new System.Windows.Forms.Timer { Interval = 60000 };
                _inactivityTimer.Tick += InactivityTimer_Tick;
                _inactivityTimer.Start();
            }

            MouseMove += (s, e) => _lastActivity = DateTime.Now;
            KeyDown += (s, e) => _lastActivity = DateTime.Now;

            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
        }

        private void AddNavSection(FlowLayoutPanel parent, string title)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = ThemeColors.SidebarMuted,
                Size = new Size(220, 24),
                Margin = new Padding(8, 12, 0, 2),
                TextAlign = ContentAlignment.BottomLeft
            };
            parent.Controls.Add(lbl);
        }

        private void AddNavButton(FlowLayoutPanel parent, string key, string label)
        {
            var btn = new ModernButton
            {
                Text = label,
                StyleType = ButtonStyleType.SidebarNav,
                Size = new Size(228, 38),
                Margin = new Padding(0, 2, 0, 2),
                Font = new Font("Segoe UI", 9.2f)
            };
            btn.Click += (s, e) => NavigateTo(key);
            parent.Controls.Add(btn);
            _navButtons[key] = btn;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            NavigateTo("Dashboard");
        }

        private void PerformGlobalSearch(string query)
        {
            var prod = _productService.GetProductByBarcode(query);
            if (prod != null)
            {
                NavigateTo("Products");
                return;
            }

            var sales = _saleService.GetInstallmentSales(search: query);
            if (sales.Any())
            {
                NavigateTo("Sales");
                return;
            }

            NavigateTo("Tenants");
            if (_viewCache.TryGetValue("Tenants", out var v) && v is TenantManagementView tv)
            {
                tv.LoadTenants();
            }
        }

        public void NavigateTo(string key, int? parameterId = null)
        {
            foreach (var kvp in _navButtons)
            {
                kvp.Value.IsSelected = (kvp.Key == key);
            }

            UserControl view;
            if (!_viewCache.TryGetValue(key, out view!))
            {
                view = CreateView(key);
                _viewCache[key] = view;
            }

            pnlContent.SuspendLayout();
            pnlContent.Controls.Clear();
            view.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(view);
            pnlContent.ResumeLayout(true);

            _currentViewKey = key;
            lblPageTitle.Text = GetPageTitle(key);

            if (key == "Profiles" && parameterId.HasValue && view is CustomerCreditProfileView cpv)
            {
                cpv.SelectCustomer(parameterId.Value);
            }

            // Trigger refresh
            if (view is DashboardView dv) dv.RefreshDashboard();
            else if (view is InstallmentSaleView isv) isv.RefreshData();
            else if (view is ProductManagementView pmv) pmv.RefreshData();
            else if (view is DefaultersView dfv) dfv.RefreshData();
            else if (view is CustomerCreditProfileView cpv2 && !parameterId.HasValue) cpv2.LoadCustomers();
            else if (view is RegisterView rv) rv.LoadRegisterData();
            else if (view is PropertyManagementView pv) pv.LoadProperties();
            else if (view is TenantManagementView tv) tv.LoadTenants();
            else if (view is MonthlyRentProcessingView mv) mv.LoadSchedules();
            else if (view is LedgerView lv) lv.LoadLedger();
            else if (view is ReportsView rpv) rpv.GenerateReport();
            else if (view is BackupRestoreView bv) bv.LoadHistory();
            else if (view is UserManagementView uv) uv.LoadUsers();
            else if (view is SettingsView sv) sv.LoadSettingsValues();
        }

        private UserControl CreateView(string key)
        {
            int currentUserId = AuthService.CurrentUser?.Id ?? 1;
            string currentUsername = AuthService.CurrentUser?.Username ?? "Admin";

            return key switch
            {
                "Dashboard" => new DashboardView(target => NavigateTo(target)),
                "Sales" => new InstallmentSaleView(_saleService, _productService, currentUserId, currentUsername),
                "Products" => new ProductManagementView(_productService, _inventoryService, currentUserId, currentUsername),
                "Defaulters" => new DefaultersView(_receivablesService, _saleService, currentUserId, currentUsername)
                {
                    OnOpenCustomerProfileRequested = custId => NavigateTo("Profiles", custId)
                },
                "Profiles" => new CustomerCreditProfileView(_receivablesService, _saleService, _productService, currentUserId, currentUsername),
                "Register" => new RegisterView(),
                "Properties" => new PropertyManagementView(),
                "Tenants" => new TenantManagementView((target, id) => NavigateTo(target, id)),
                "Billing" => new MonthlyRentProcessingView(),
                "Ledger" => new LedgerView(),
                "Reports" => new ReportsView(),
                "Backup" => new BackupRestoreView(),
                "ImportExport" => new DataImportExportView(),
                "Users" => new UserManagementView(),
                "Settings" => new SettingsView(),
                _ => new DashboardView()
            };
        }

        private string GetPageTitle(string key)
        {
            return key switch
            {
                "Dashboard" => "Operations & Receivables Dashboard",
                "Sales" => "Installment, Retail & BNPL Sales Engine",
                "Products" => "Product Catalog & Stock Inventory Management",
                "Defaulters" => "Universal Defaulters & Overdue Receivables Aging",
                "Profiles" => "360° Customer Credit Profile & Portfolio History",
                "Register" => "Universal Receivables & Rent Register",
                "Properties" => "Property & Unit Management",
                "Tenants" => "Customer / Tenant Profiles & Directory",
                "Billing" => "Monthly Rent & Recurring Receivables Processing",
                "Ledger" => "Universal Customer Ledger & Statement of Accounts",
                "Reports" => "Reports, Financial Statements & Analytics",
                "Backup" => "Database Backup & Safe Restore",
                "ImportExport" => "CSV Data Migration Wizard",
                "Users" => "User Management & Security Roles",
                "Settings" => "System Settings & Business Rules Configuration",
                _ => "Installment & Receivables Management Platform"
            };
        }

        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            int autoLockMins = SettingService.GetInt("Security.AutoLockMinutes", 15);
            if (autoLockMins <= 0) return;

            if ((DateTime.Now - _lastActivity).TotalMinutes >= autoLockMins)
            {
                _inactivityTimer?.Stop();
                AuditService.Log("Auto Lock", "Session", AuthService.CurrentUser?.Id.ToString(), "Session locked due to inactivity.");

                ModernMessageBox.ShowInfo("Session locked due to inactivity. Please sign in again to continue.", "Auto Lock", this);

                AuthService.Logout();
                Hide();
                using var login = new LoginForm();
                if (login.ShowDialog() == DialogResult.OK)
                {
                    _lastActivity = DateTime.Now;
                    _inactivityTimer?.Start();
                    Show();
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (SettingService.GetBool("Backup.AutoBackupOnExit", true))
            {
                try
                {
                    BackupService.CreateBackup(null, BackupType.AutoOnExit, "Automated backup on application exit");
                }
                catch { }
            }
        }
    }
}
