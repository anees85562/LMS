using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class DefaultersView : UserControl
    {
        private readonly ReceivablesService _receivablesService;
        private readonly InstallmentSaleService _saleService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private TextBox _txtSearch = null!;
        private ComboBox _cboBucket = null!;
        private ModernButton _btnPrintReport = null!;
        private ModernButton _btnViewCreditProfile = null!;
        private ModernDataGridView _grid = null!;

        private StatCardControl _cardDefaultersCount = null!;
        private StatCardControl _cardTotalOverdue = null!;
        private StatCardControl _cardTotalOutstanding = null!;
        private StatCardControl _cardCriticalCount = null!;

        public Action<int>? OnOpenCustomerProfileRequested;

        public DefaultersView(
            ReceivablesService receivablesService,
            InstallmentSaleService saleService,
            int currentUserId,
            string currentUsername)
        {
            _receivablesService = receivablesService;
            _saleService = saleService;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;

            InitializeComponent();
            RefreshData();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                "⚠️ Universal Defaulters & Overdue Receivables Aging",
                "Real-time monitoring of delinquent customers, overdue installment dues, rent arrears, risk ratings, and aging buckets (1-7d, 8-30d, 31-60d, 60+d)."
            );
            Controls.Add(pnlHeader);

            // 2. Filter Bar
            var filterBar = UIHelper.CreateFilterBar(54);
            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search Defaulters by Name, Phone, CNIC, Account, Code...",
                Width = 320,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 8, 0)
            };
            _txtSearch.TextChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_txtSearch);

            _cboBucket = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 160,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 12, 0)
            };
            _cboBucket.Items.Add("All Aging Buckets");
            _cboBucket.Items.Add("1-7 Days");
            _cboBucket.Items.Add("8-30 Days");
            _cboBucket.Items.Add("31-60 Days");
            _cboBucket.Items.Add("60+ Days");
            _cboBucket.SelectedIndex = 0;
            _cboBucket.SelectedIndexChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_cboBucket);

            _btnPrintReport = new ModernButton
            {
                Text = "🖨 Print Defaulter Report",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(180, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnPrintReport.Click += BtnPrintReport_Click;
            topFlow.Controls.Add(_btnPrintReport);

            _btnViewCreditProfile = new ModernButton
            {
                Text = "👤 Customer Credit Profile",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(190, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            _btnViewCreditProfile.Click += BtnViewCreditProfile_Click;
            topFlow.Controls.Add(_btnViewCreditProfile);

            filterBar.Controls.Add(topFlow);
            Controls.Add(filterBar);

            // 3. Main Grid Card
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            _grid = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _grid.Columns.Add("CustomerId", "CustID");
            _grid.Columns["CustomerId"].Visible = false;
            _grid.Columns.Add("Code", "Customer Code");
            _grid.Columns.Add("Name", "Customer / Party Name");
            _grid.Columns.Add("Phone", "Contact Phone");
            _grid.Columns.Add("Details", "Product / Property Reference");
            _grid.Columns.Add("Bucket", "Aging Bucket");
            _grid.Columns.Add("DaysOverdue", "Days Overdue");
            _grid.Columns.Add("MissedCount", "Missed Dues");
            _grid.Columns.Add("OverdueAmt", "Overdue Amount");
            _grid.Columns.Add("Outstanding", "Total Outstanding");
            _grid.Columns.Add("Rating", "Risk Rating");

            _grid.CellDoubleClick += (s, e) => BtnViewCreditProfile_Click(s, e);
            pnlGridCard.Controls.Add(_grid);
            Controls.Add(pnlGridCard);

            // 4. Bottom KPI Cards Flow
            var bottomFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            _cardDefaultersCount = new StatCardControl
            {
                Title = "Total Defaulters",
                Value = "0",
                Subtitle = "Overdue Accounts",
                IconSymbol = "⚠️",
                AccentColor = ThemeColors.Warning,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardDefaultersCount);

            _cardTotalOverdue = new StatCardControl
            {
                Title = "Overdue Amount",
                Value = "Rs. 0",
                Subtitle = "Past Due Balance",
                IconSymbol = "🚨",
                AccentColor = ThemeColors.Danger,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalOverdue);

            _cardTotalOutstanding = new StatCardControl
            {
                Title = "Portfolio Outstanding",
                Value = "Rs. 0",
                Subtitle = "Total Remaining",
                IconSymbol = "💼",
                AccentColor = ThemeColors.Info,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalOutstanding);

            _cardCriticalCount = new StatCardControl
            {
                Title = "Critical (60+ Days)",
                Value = "0",
                Subtitle = "High Risk Accounts",
                IconSymbol = "🛑",
                AccentColor = ThemeColors.DangerText,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 0, 0)
            };
            bottomFlow.Controls.Add(_cardCriticalCount);

            Controls.Add(bottomFlow);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();
        }

        public void RefreshData()
        {
            string? search = string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim();
            string? bucket = _cboBucket.SelectedItem?.ToString() == "All Aging Buckets" ? null : _cboBucket.SelectedItem?.ToString();

            var defaulters = _receivablesService.GetDefaultersList(minDays: 1, maxDays: null, search: search, bucketFilter: bucket);

            _grid.Rows.Clear();
            decimal totalOverdue = 0;
            decimal totalOutstanding = 0;
            int criticalCount = 0;

            foreach (var d in defaulters)
            {
                _grid.Rows.Add(
                    d.CustomerId,
                    d.CustomerCode,
                    d.FullName,
                    d.ContactNumber,
                    d.ReferenceDetails,
                    d.Bucket,
                    d.DaysOverdue,
                    d.MissedInstallmentsCount,
                    SettingService.FormatCurrency(d.OverdueAmount),
                    SettingService.FormatCurrency(d.TotalOutstanding),
                    d.Rating
                );

                totalOverdue += d.OverdueAmount;
                totalOutstanding += d.TotalOutstanding;
                if (d.DaysOverdue >= 60) criticalCount++;
            }

            _cardDefaultersCount.Value = defaulters.Count.ToString();
            _cardTotalOverdue.Value = SettingService.FormatCurrency(totalOverdue);
            _cardTotalOutstanding.Value = SettingService.FormatCurrency(totalOutstanding);
            _cardCriticalCount.Value = criticalCount.ToString();
        }

        private int? GetSelectedCustomerId()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Cells["CustomerId"].Value is int id)
            {
                return id;
            }
            return null;
        }

        private void BtnPrintReport_Click(object? sender, EventArgs e)
        {
            var ds = ReportService.GetDefaultersReport();
            PrintingService.PrintReport(ds, showPreview: true);
        }

        private void BtnViewCreditProfile_Click(object? sender, EventArgs e)
        {
            int? custId = GetSelectedCustomerId();
            if (custId.HasValue && custId.Value > 0)
            {
                OnOpenCustomerProfileRequested?.Invoke(custId.Value);
            }
            else
            {
                ModernMessageBox.ShowInfo("Please select a customer from the defaulters list.", "Selection Required", this);
            }
        }
    }
}
