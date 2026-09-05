using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class DashboardView : UserControl
    {
        private StatCardControl card1 = null!;
        private StatCardControl card2 = null!;
        private StatCardControl card3 = null!;
        private StatCardControl card4 = null!;

        private FlowLayoutPanel pnlMainFlow = null!;
        private Panel pnlHeader = null!;
        private FlowLayoutPanel pnlCards = null!;
        private Panel pnlActionsCard = null!;
        private FlowLayoutPanel pnlActionsFlow = null!;
        private Panel pnlAlertsSection = null!;
        private FlowLayoutPanel pnlAlerts = null!;
        private Panel pnlRecentSection = null!;
        private Panel pnlGridCard = null!;
        private ModernDataGridView dgvRecentActivity = null!;
        private Action<string>? _navigationCallback;

        private readonly TerminologyService _terminology = new TerminologyService(new SettingService());
        private readonly ReceivablesService _receivablesService = new ReceivablesService();

        public DashboardView(Action<string>? navigationCallback = null)
        {
            _navigationCallback = navigationCallback;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            AutoScroll = true;
            Padding = new Padding(24, 16, 24, 24);

            pnlMainFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            // 1. Header
            pnlHeader = UIHelper.CreatePageHeader(
                "Business Operations & Receivables Dashboard",
                $"Real-time receivables, installment collections, alerts and operational metrics as of {DateTime.Now:dddd, MMMM dd, yyyy}"
            );
            pnlHeader.Margin = new Padding(0, 0, 0, 16);
            pnlMainFlow.Controls.Add(pnlHeader);

            // 2. KPI Stat Cards Row
            pnlCards = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };

            card1 = new StatCardControl
            {
                Title = "Active Accounts",
                Value = "0",
                Subtitle = "Customers / Deals",
                IconSymbol = "👥",
                AccentColor = ThemeColors.Primary,
                Width = 260,
                Height = 110,
                Margin = new Padding(0, 0, 16, 12)
            };
            pnlCards.Controls.Add(card1);

            card2 = new StatCardControl
            {
                Title = "Due Today / Month Demands",
                Value = "Rs. 0",
                Subtitle = "Due Demanded",
                IconSymbol = "💰",
                AccentColor = ThemeColors.Info,
                Width = 260,
                Height = 110,
                Margin = new Padding(0, 0, 16, 12)
            };
            pnlCards.Controls.Add(card2);

            card3 = new StatCardControl
            {
                Title = "Recoveries / Collections",
                Value = "Rs. 0",
                Subtitle = "Collected This Month",
                IconSymbol = "✅",
                AccentColor = ThemeColors.Success,
                Width = 260,
                Height = 110,
                Margin = new Padding(0, 0, 16, 12)
            };
            pnlCards.Controls.Add(card3);

            card4 = new StatCardControl
            {
                Title = "Total Outstanding / Overdue",
                Value = "Rs. 0",
                Subtitle = "Portfolio Balance",
                IconSymbol = "⚠️",
                AccentColor = ThemeColors.Danger,
                Width = 260,
                Height = 110,
                Margin = new Padding(0, 0, 16, 12)
            };
            pnlCards.Controls.Add(card4);

            pnlMainFlow.Controls.Add(pnlCards);

            // 3. Quick Actions Card
            pnlActionsCard = UIHelper.CreateCardPanel(new Padding(12, 10, 12, 10));
            pnlActionsCard.Height = 65;
            pnlActionsCard.Margin = new Padding(0, 0, 0, 16);

            var lblAct = new Label
            {
                Text = "QUICK ACTIONS:",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = ThemeColors.TextMuted,
                Location = new Point(14, 22),
                AutoSize = true
            };
            pnlActionsCard.Controls.Add(lblAct);

            pnlActionsFlow = new FlowLayoutPanel
            {
                Location = new Point(125, 12),
                Size = new Size(950, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            pnlActionsCard.Controls.Add(pnlActionsFlow);
            pnlMainFlow.Controls.Add(pnlActionsCard);

            // 4. Alerts Center
            pnlAlertsSection = UIHelper.CreateSectionHeader("🔔 Actionable Business Alerts & Reminders");
            pnlAlertsSection.Height = 30;
            pnlAlertsSection.Margin = new Padding(0, 0, 0, 4);
            pnlMainFlow.Controls.Add(pnlAlertsSection);

            pnlAlerts = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            pnlMainFlow.Controls.Add(pnlAlerts);

            // 5. Recent Transactions
            pnlRecentSection = UIHelper.CreateSectionHeader("📜 Recent Financial Transactions, Invoices & Collections");
            pnlRecentSection.Height = 30;
            pnlRecentSection.Margin = new Padding(0, 0, 0, 4);
            pnlMainFlow.Controls.Add(pnlRecentSection);

            pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Height = 320;
            pnlGridCard.Margin = new Padding(0, 0, 0, 20);

            dgvRecentActivity = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            pnlGridCard.Controls.Add(dgvRecentActivity);
            pnlMainFlow.Controls.Add(pnlGridCard);

            Controls.Add(pnlMainFlow);

            Resize += DashboardView_Resize;
            Load += (s, e) => { UpdateLayoutWidths(); RefreshDashboard(); };
        }

        private void DashboardView_Resize(object? sender, EventArgs e)
        {
            UpdateLayoutWidths();
        }

        private void UpdateLayoutWidths()
        {
            int w = Math.Max(900, ClientSize.Width - Padding.Horizontal - 8);
            if (pnlHeader != null) pnlHeader.Width = w;
            if (pnlCards != null) pnlCards.Width = w;
            if (pnlActionsCard != null)
            {
                pnlActionsCard.Width = w;
                pnlActionsFlow.Width = Math.Max(700, w - 140);
            }
            if (pnlAlertsSection != null) pnlAlertsSection.Width = w;
            if (pnlAlerts != null) pnlAlerts.Width = w;
            if (pnlRecentSection != null) pnlRecentSection.Width = w;
            if (pnlGridCard != null) pnlGridCard.Width = w;
        }

        private void RebuildQuickActions()
        {
            pnlActionsFlow.Controls.Clear();

            var btnNewSale = new ModernButton
            {
                Text = "+ New Sale / Invoice",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(160, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnNewSale.Click += (s, e) => _navigationCallback?.Invoke("Sales");
            pnlActionsFlow.Controls.Add(btnNewSale);

            var btnPay = new ModernButton
            {
                Text = "💳 Record Payment",
                StyleType = ButtonStyleType.Success,
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnPay.Click += (s, e) =>
            {
                using var dlg = new RecordPaymentForm();
                if (dlg.ShowDialog() == DialogResult.OK) RefreshDashboard();
            };
            pnlActionsFlow.Controls.Add(btnPay);

            var btnAddProd = new ModernButton
            {
                Text = "📦 Products / Stock",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnAddProd.Click += (s, e) => _navigationCallback?.Invoke("Products");
            pnlActionsFlow.Controls.Add(btnAddProd);

            var btnDefaulters = new ModernButton
            {
                Text = "⚠️ Defaulters Aging",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnDefaulters.Click += (s, e) => _navigationCallback?.Invoke("Defaulters");
            pnlActionsFlow.Controls.Add(btnDefaulters);

            var btnRegister = new ModernButton
            {
                Text = "📖 Universal Register",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnRegister.Click += (s, e) => _navigationCallback?.Invoke("Register");
            pnlActionsFlow.Controls.Add(btnRegister);

            var btnBackup = new ModernButton
            {
                Text = "💾 Backup",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(100, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnBackup.Click += (s, e) =>
            {
                var res = BackupService.CreateBackup(null, BackupType.Manual);
                if (res.Success)
                    ModernMessageBox.ShowInfo(res.Message, "Backup Success", this);
                else
                    ModernMessageBox.ShowError(res.Message, "Backup Failed", this);
                RefreshDashboard();
            };
            pnlActionsFlow.Controls.Add(btnBackup);
        }

        public void RefreshDashboard()
        {
            RebuildQuickActions();

            var mode = _terminology.GetActiveBusinessType();
            var metrics = _receivablesService.GetReceivablesDashboardMetrics();

            if (mode == BusinessType.PropertyRent)
            {
                var occ = PropertyService.GetOccupancyMetrics();
                card1.Title = "Properties & Occupancy";
                card1.Value = $"{occ.TotalUnits} Units";
                card1.Subtitle = $"{occ.OccupiedUnits} Occupied ({occ.OccupancyRate:N0}%) | {occ.VacantUnits} Vacant";

                DateTime now = DateTime.Now;
                var rentSum = BillingService.GetMonthlyRentSummary(now.Year, now.Month);
                card2.Title = "Expected Rent This Month";
                card2.Value = SettingService.FormatCurrency(rentSum.Expected);
                card2.Subtitle = $"{now:MMMM yyyy} Billing";

                card3.Title = "Rent Collected";
                card3.Value = SettingService.FormatCurrency(rentSum.Received);
                card3.Subtitle = $"{rentSum.PaidCount} Fully Paid | {rentSum.PartialCount} Partial";

                card4.Title = "Pending & Overdue Rent";
                card4.Value = SettingService.FormatCurrency(rentSum.Pending);
                card4.Subtitle = $"{rentSum.OverdueCount} Overdue ({SettingService.FormatCurrency(rentSum.Overdue)})";
            }
            else
            {
                card1.Title = "Active Customers / Parties";
                card1.Value = $"{metrics.TotalCustomers} Active";
                card1.Subtitle = "Registered customer accounts";

                card2.Title = "Receivables Due Today";
                card2.Value = SettingService.FormatCurrency(metrics.DueToday);
                card2.Subtitle = "Instalments / rent due today";

                card3.Title = "Month Collections / Recoveries";
                card3.Value = SettingService.FormatCurrency(metrics.MonthCollection);
                card3.Subtitle = $"{DateTime.Now:MMMM yyyy} Total Recoveries";

                card4.Title = "Total Outstanding Receivables";
                card4.Value = SettingService.FormatCurrency(metrics.TotalOutstanding);
                card4.Subtitle = $"Overdue: {SettingService.FormatCurrency(metrics.TotalOverdue)}";
            }

            // Alerts
            pnlAlerts.Controls.Clear();
            var alerts = AlertService.GenerateActiveAlerts();
            if (alerts.Count == 0)
            {
                var lblNone = new Label
                {
                    Text = "✅ Everything is up to date. No pending alerts or overdue payments!",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = ThemeColors.SuccessText,
                    AutoSize = true,
                    Margin = new Padding(8, 8, 8, 12)
                };
                pnlAlerts.Controls.Add(lblNone);
            }
            else
            {
                foreach (var a in alerts.Take(6))
                {
                    var banner = new AlertBannerControl
                    {
                        Title = a.Title,
                        MessageText = a.Message,
                        Severity = a.Severity,
                        ActionText = a.ActionKey == "InstallmentPayment" ? "Collect" : (a.ActionKey == "Payment" ? "Pay Now" : (a.ActionKey == "Products" ? "Inventory" : "View")),
                        Width = 510,
                        Margin = new Padding(0, 0, 14, 10),
                        OnActionClick = () =>
                        {
                            if (a.ActionKey == "InstallmentPayment" || a.ActionKey == "Payment")
                            {
                                _navigationCallback?.Invoke("Sales");
                            }
                            else if (a.ActionKey == "Products")
                            {
                                _navigationCallback?.Invoke("Products");
                            }
                            else if (a.ActionKey == "CustomerProfile")
                            {
                                _navigationCallback?.Invoke("Defaulters");
                            }
                            else if (a.ActionKey == "Backup")
                            {
                                _navigationCallback?.Invoke("Backup");
                            }
                            else
                            {
                                _navigationCallback?.Invoke("Tenants");
                            }
                        }
                    };
                    pnlAlerts.Controls.Add(banner);
                }
            }

            // Recent Transactions Grid
            using var db = new AppDbContext();
            var recentTxs = db.Transactions
                              .Include(t => t.Tenant)
                              .Include(t => t.PropertyUnit).ThenInclude(u => u!.Property)
                              .Include(t => t.InstallmentSale)
                              .Where(t => !t.IsVoided)
                              .OrderByDescending(t => t.TransactionDate)
                              .ThenByDescending(t => t.Id)
                              .Take(15)
                              .Select(t => new
                              {
                                  Date = t.TransactionDate.ToString("dd/MM/yyyy"),
                                  Code = t.TransactionCode,
                                  Type = t.TransactionType.ToString(),
                                  Party = t.Tenant != null ? t.Tenant.FullName : "-",
                                  Reference = t.InstallmentSale != null ? t.InstallmentSale.InvoiceNumber : (t.PropertyUnit != null ? $"{t.PropertyUnit.Property!.Name} - {t.PropertyUnit.UnitNumber}" : "-"),
                                  Description = t.Description,
                                  Debit = t.Debit > 0 ? t.Debit.ToString("N0") : "-",
                                  Credit = t.Credit > 0 ? t.Credit.ToString("N0") : "-",
                                  Method = t.PaymentMethod.ToString()
                              })
                              .ToList();

            dgvRecentActivity.DataSource = recentTxs;
            if (dgvRecentActivity.Columns.Count > 0 && dgvRecentActivity.Columns.Contains("Description"))
            {
                dgvRecentActivity.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }
    }
}
