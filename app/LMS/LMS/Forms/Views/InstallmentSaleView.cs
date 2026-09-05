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
    public class InstallmentSaleView : UserControl
    {
        private readonly InstallmentSaleService _saleService;
        private readonly ProductService _productService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private TextBox _txtSearch = null!;
        private ComboBox _cboSaleType = null!;
        private ComboBox _cboStatus = null!;
        private ModernButton _btnNewSale = null!;
        private ModernButton _btnCollectPayment = null!;
        private ModernButton _btnPrintInvoice = null!;
        private ModernButton _btnCustomerStatement = null!;
        private ModernDataGridView _gridSales = null!;
        private ModernDataGridView _gridSchedules = null!;

        private StatCardControl _cardActiveAccounts = null!;
        private StatCardControl _cardTotalSales = null!;
        private StatCardControl _cardTotalRecovered = null!;
        private StatCardControl _cardTotalOutstanding = null!;

        public InstallmentSaleView(
            InstallmentSaleService saleService,
            ProductService productService,
            int currentUserId,
            string currentUsername)
        {
            _saleService = saleService;
            _productService = productService;
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
                "🛒 Installment, Retail & BNPL Sales Engine",
                "Create point-of-sale customer installment agreements, credit contracts, track repayment schedules, and collect payments."
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
                PlaceholderText = "Search by Invoice #, Customer, CNIC, Item, Serial...",
                Width = 280,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 8, 0)
            };
            _txtSearch.TextChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_txtSearch);

            _cboSaleType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 140,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 8, 0)
            };
            _cboSaleType.Items.Add("All Sale Types");
            _cboSaleType.Items.Add(SaleType.InstallmentSale);
            _cboSaleType.Items.Add(SaleType.BNPLSale);
            _cboSaleType.Items.Add(SaleType.CreditSale);
            _cboSaleType.Items.Add(SaleType.CashSale);
            _cboSaleType.SelectedIndex = 0;
            _cboSaleType.SelectedIndexChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_cboSaleType);

            _cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 12, 0)
            };
            _cboStatus.Items.Add("All Statuses");
            _cboStatus.Items.Add(InstallmentPlanStatus.Active);
            _cboStatus.Items.Add(InstallmentPlanStatus.PartiallyPaid);
            _cboStatus.Items.Add(InstallmentPlanStatus.Completed);
            _cboStatus.Items.Add(InstallmentPlanStatus.Overdue);
            _cboStatus.Items.Add(InstallmentPlanStatus.Settled);
            _cboStatus.SelectedIndex = 0;
            _cboStatus.SelectedIndexChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_cboStatus);

            _btnNewSale = new ModernButton
            {
                Text = "+ New Sale",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(115, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnNewSale.Click += BtnNewSale_Click;
            topFlow.Controls.Add(_btnNewSale);

            _btnCollectPayment = new ModernButton
            {
                Text = "💵 Collect Due",
                StyleType = ButtonStyleType.Success,
                Size = new Size(125, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnCollectPayment.Click += BtnCollectPayment_Click;
            topFlow.Controls.Add(_btnCollectPayment);

            _btnPrintInvoice = new ModernButton
            {
                Text = "🖨 Print Invoice",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(120, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnPrintInvoice.Click += BtnPrintInvoice_Click;
            topFlow.Controls.Add(_btnPrintInvoice);

            _btnCustomerStatement = new ModernButton
            {
                Text = "📄 Statement",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(110, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            _btnCustomerStatement.Click += BtnCustomerStatement_Click;
            topFlow.Controls.Add(_btnCustomerStatement);

            filterBar.Controls.Add(topFlow);
            Controls.Add(filterBar);

            // 3. Main Split Area: Master Sales Grid & Schedule Breakdown
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280,
                BackColor = ThemeColors.Border,
                Margin = new Padding(0, 10, 0, 10)
            };

            // Master Sales Grid Panel
            var pnlMasterCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlMasterCard.Dock = DockStyle.Fill;

            _gridSales = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridSales.Columns.Add("Id", "ID");
            _gridSales.Columns["Id"].Visible = false;
            _gridSales.Columns.Add("InvoiceNo", "Invoice #");
            _gridSales.Columns.Add("Date", "Sale Date");
            _gridSales.Columns.Add("Customer", "Customer Name");
            _gridSales.Columns.Add("Phone", "Contact");
            _gridSales.Columns.Add("SaleType", "Sale Type");
            _gridSales.Columns.Add("Items", "Items / Products");
            _gridSales.Columns.Add("NetPrice", "Net Price");
            _gridSales.Columns.Add("DownPayment", "Down Payment");
            _gridSales.Columns.Add("TotalPaid", "Total Paid");
            _gridSales.Columns.Add("Remaining", "Remaining Balance");
            _gridSales.Columns.Add("Status", "Plan Status");

            _gridSales.SelectionChanged += (s, e) => LoadSelectedSaleSchedules();
            _gridSales.CellDoubleClick += (s, e) => BtnCollectPayment_Click(s, e);
            pnlMasterCard.Controls.Add(_gridSales);
            split.Panel1.Controls.Add(pnlMasterCard);

            // Schedules Sub-Grid Panel
            var pnlSubCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlSubCard.Dock = DockStyle.Fill;

            var pnlSchTitle = UIHelper.CreateSectionHeader("📋 Installment Schedule Breakdown for Selected Account");
            pnlSchTitle.Height = 28;
            pnlSubCard.Controls.Add(pnlSchTitle);

            _gridSchedules = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridSchedules.Columns.Add("No", "Inst #");
            _gridSchedules.Columns.Add("DueDate", "Due Date");
            _gridSchedules.Columns.Add("DueAmount", "Due Amount");
            _gridSchedules.Columns.Add("PaidAmount", "Paid Amount");
            _gridSchedules.Columns.Add("Remaining", "Remaining");
            _gridSchedules.Columns.Add("Status", "Status");
            _gridSchedules.Columns.Add("PaidDate", "Paid Date");

            pnlSubCard.Controls.Add(_gridSchedules);
            _gridSchedules.BringToFront();
            split.Panel2.Controls.Add(pnlSubCard);

            Controls.Add(split);

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

            _cardActiveAccounts = new StatCardControl
            {
                Title = "Active Installments",
                Value = "0",
                Subtitle = "Accounts Paying",
                IconSymbol = "👥",
                AccentColor = ThemeColors.Primary,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardActiveAccounts);

            _cardTotalSales = new StatCardControl
            {
                Title = "Sales Valuation",
                Value = "Rs. 0",
                Subtitle = "Total Portfolio Value",
                IconSymbol = "🛒",
                AccentColor = ThemeColors.Info,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalSales);

            _cardTotalRecovered = new StatCardControl
            {
                Title = "Total Recovered",
                Value = "Rs. 0",
                Subtitle = "Cash & Installments",
                IconSymbol = "✅",
                AccentColor = ThemeColors.Success,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalRecovered);

            _cardTotalOutstanding = new StatCardControl
            {
                Title = "Total Outstanding",
                Value = "Rs. 0",
                Subtitle = "Receivables Due",
                IconSymbol = "⚠️",
                AccentColor = ThemeColors.Danger,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 0, 0)
            };
            bottomFlow.Controls.Add(_cardTotalOutstanding);

            Controls.Add(bottomFlow);

            // Set correct stacking order
            split.SendToBack();
            pnlHeader.BringToFront();
        }

        public void RefreshData()
        {
            string? search = string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim();
            SaleType? st = _cboSaleType.SelectedItem is SaleType type ? type : null;
            InstallmentPlanStatus? status = _cboStatus.SelectedItem is InstallmentPlanStatus ps ? ps : null;

            var sales = _saleService.GetInstallmentSales(search, st, status);

            _gridSales.Rows.Clear();
            decimal totalNet = 0;
            decimal totalPaid = 0;
            decimal totalRemaining = 0;
            int activeCount = 0;

            foreach (var s in sales)
            {
                string itemsStr = string.Join(", ", s.Items.Select(i => i.ItemDescription));
                _gridSales.Rows.Add(
                    s.Id,
                    s.InvoiceNumber,
                    s.SaleDate.ToString("dd/MM/yyyy"),
                    s.Customer?.FullName ?? "",
                    s.Customer?.ContactNumber ?? "",
                    s.SaleType.ToString(),
                    itemsStr,
                    SettingService.FormatCurrency(s.NetSalePrice),
                    SettingService.FormatCurrency(s.DownPayment),
                    SettingService.FormatCurrency(s.TotalPaid),
                    SettingService.FormatCurrency(s.RemainingBalance),
                    s.Status.ToString()
                );

                totalNet += s.NetSalePrice;
                totalPaid += s.TotalPaid;
                totalRemaining += s.RemainingBalance;
                if (s.Status == InstallmentPlanStatus.Active || s.Status == InstallmentPlanStatus.PartiallyPaid || s.Status == InstallmentPlanStatus.Overdue)
                {
                    activeCount++;
                }
            }

            _cardActiveAccounts.Value = activeCount.ToString();
            _cardTotalSales.Value = SettingService.FormatCurrency(totalNet);
            _cardTotalRecovered.Value = SettingService.FormatCurrency(totalPaid);
            _cardTotalOutstanding.Value = SettingService.FormatCurrency(totalRemaining);

            LoadSelectedSaleSchedules();
        }

        private InstallmentSale? GetSelectedSale()
        {
            if (_gridSales.CurrentRow != null && _gridSales.CurrentRow.Cells["Id"].Value is int id)
            {
                return _saleService.GetInstallmentSaleById(id);
            }
            return null;
        }

        private void LoadSelectedSaleSchedules()
        {
            _gridSchedules.Rows.Clear();
            var sale = GetSelectedSale();
            if (sale == null) return;

            foreach (var sch in sale.Schedules.OrderBy(s => s.InstallmentNumber))
            {
                _gridSchedules.Rows.Add(
                    $"#{sch.InstallmentNumber}",
                    sch.DueDate.ToString("dd/MM/yyyy"),
                    SettingService.FormatCurrency(sch.DueAmount),
                    SettingService.FormatCurrency(sch.PaidAmount),
                    SettingService.FormatCurrency(sch.RemainingAmount),
                    sch.Status.ToString(),
                    sch.PaidDate.HasValue ? sch.PaidDate.Value.ToString("dd/MM/yyyy") : "-"
                );
            }
        }

        private void BtnNewSale_Click(object? sender, EventArgs e)
        {
            using var dlg = new NewInstallmentSaleForm(_saleService, _productService, _currentUserId, _currentUsername);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnCollectPayment_Click(object? sender, EventArgs e)
        {
            var sale = GetSelectedSale();
            if (sale == null)
            {
                ModernMessageBox.ShowInfo("Please select an installment account from the list.", "Selection Required", this);
                return;
            }

            if (sale.RemainingBalance <= 0)
            {
                ModernMessageBox.ShowInfo("This sale is already fully paid.", "Notice", this);
                return;
            }

            using var dlg = new InstallmentPaymentDialog(_saleService, sale, _currentUserId, _currentUsername);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnPrintInvoice_Click(object? sender, EventArgs e)
        {
            var sale = GetSelectedSale();
            if (sale == null)
            {
                ModernMessageBox.ShowInfo("Please select an invoice to print.", "Selection Required", this);
                return;
            }

            PrintingService.PrintInvoice(sale, isReprint: false, showPreview: true);
        }

        private void BtnCustomerStatement_Click(object? sender, EventArgs e)
        {
            var sale = GetSelectedSale();
            if (sale == null || sale.CustomerId <= 0) return;

            var ds = ReportService.GetTenantStatementReport(sale.CustomerId);
            PrintingService.PrintReport(ds, showPreview: true);
        }
    }
}
