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
    public class CustomerCreditProfileView : UserControl
    {
        private readonly ReceivablesService _receivablesService;
        private readonly InstallmentSaleService _saleService;
        private readonly ProductService _productService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private ComboBox _cboCustomerPicker = null!;
        private ModernButton _btnStatement = null!;
        private ModernButton _btnNewSale = null!;

        // KPI Labels
        private Label _lblCreditLimit = null!;
        private Label _lblAvailCredit = null!;
        private Label _lblTotalPurchases = null!;
        private Label _lblTotalPaid = null!;
        private Label _lblOutstanding = null!;
        private Label _lblOverdue = null!;
        private Label _lblRating = null!;

        // Info labels
        private Label _lblCustomerName = null!;
        private Label _lblPhone = null!;
        private Label _lblAddress = null!;
        private Label _lblGuarantor = null!;

        // Tab Grids
        private TabControl _tabControl = null!;
        private ModernDataGridView _gridPlans = null!;
        private ModernDataGridView _gridLedger = null!;

        private int? _selectedCustomerId;

        public CustomerCreditProfileView(
            ReceivablesService receivablesService,
            InstallmentSaleService saleService,
            ProductService productService,
            int currentUserId,
            string currentUsername,
            int? initialCustomerId = null)
        {
            _receivablesService = receivablesService;
            _saleService = saleService;
            _productService = productService;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;
            _selectedCustomerId = initialCustomerId;

            InitializeComponent();
            LoadCustomers();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                "👤 360° Customer Credit Profile & Portfolio History",
                "Holistic view of individual customer credit limits, outstanding agreements, payment track record, guarantor details, and complete chronological statement of accounts."
            );
            Controls.Add(pnlHeader);

            // 2. Customer Selector Bar
            var filterBar = UIHelper.CreateFilterBar(54);
            var topFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            var lblPick = new Label
            {
                Text = "Select Customer / Party:",
                AutoSize = true,
                Margin = new Padding(0, 7, 8, 0),
                Font = ThemeColors.LabelBoldFont,
                ForeColor = ThemeColors.TextPrimary
            };
            topFlow.Controls.Add(lblPick);

            _cboCustomerPicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 360,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 12, 0)
            };
            _cboCustomerPicker.SelectedIndexChanged += (s, e) =>
            {
                if (_cboCustomerPicker.SelectedValue is int id && id > 0)
                {
                    SelectCustomer(id);
                }
            };
            topFlow.Controls.Add(_cboCustomerPicker);

            _btnStatement = new ModernButton
            {
                Text = "📄 Print Full Ledger",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(160, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnStatement.Click += BtnStatement_Click;
            topFlow.Controls.Add(_btnStatement);

            _btnNewSale = new ModernButton
            {
                Text = "+ New Sale for Customer",
                StyleType = ButtonStyleType.Success,
                Size = new Size(185, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            _btnNewSale.Click += BtnNewSale_Click;
            topFlow.Controls.Add(_btnNewSale);

            filterBar.Controls.Add(topFlow);
            Controls.Add(filterBar);

            // 3. Summary Area: Profile Info Card (Left) + 6-Box Matrix (Right)
            var pnlSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 145,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 10, 0, 10),
                BackColor = Color.Transparent
            };
            pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            pnlSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

            // Left Profile Card
            var boxInfoCard = UIHelper.CreateCardPanel(new Padding(14, 10, 14, 10));
            boxInfoCard.Dock = DockStyle.Fill;
            boxInfoCard.Margin = new Padding(0, 0, 8, 0);

            var infoTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Font = new Font("Segoe UI", 8.8f)
            };
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
            infoTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _lblCustomerName = new Label { Text = "-", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = ThemeColors.TextPrimary, AutoSize = true, Anchor = AnchorStyles.Left };
            infoTable.Controls.Add(new Label { Text = "Customer:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary }, 0, 0);
            infoTable.Controls.Add(_lblCustomerName, 1, 0);

            _lblPhone = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            infoTable.Controls.Add(new Label { Text = "Phone / CNIC:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary }, 0, 1);
            infoTable.Controls.Add(_lblPhone, 1, 1);

            _lblAddress = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary };
            infoTable.Controls.Add(new Label { Text = "Address / City:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary }, 0, 2);
            infoTable.Controls.Add(_lblAddress, 1, 2);

            _lblGuarantor = new Label { Text = "-", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary };
            infoTable.Controls.Add(new Label { Text = "Guarantor:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary }, 0, 3);
            infoTable.Controls.Add(_lblGuarantor, 1, 3);

            _lblRating = new Label { Text = "Rating: Good", Font = new Font("Segoe UI", 8.8f, FontStyle.Bold), ForeColor = ThemeColors.Success, AutoSize = true, Anchor = AnchorStyles.Left };
            infoTable.Controls.Add(new Label { Text = "Risk Rating:", AutoSize = true, Anchor = AnchorStyles.Left, Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextSecondary }, 0, 4);
            infoTable.Controls.Add(_lblRating, 1, 4);

            boxInfoCard.Controls.Add(infoTable);
            pnlSummary.Controls.Add(boxInfoCard, 0, 0);

            // Right 6-Card Matrix
            var kpiMatrix = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Margin = new Padding(4, 0, 0, 0)
            };
            kpiMatrix.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiMatrix.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiMatrix.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            kpiMatrix.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            kpiMatrix.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            _lblCreditLimit = CreateKpiCard(kpiMatrix, 0, 0, "CREDIT LIMIT", "Rs. 0", Color.FromArgb(30, 41, 59));
            _lblAvailCredit = CreateKpiCard(kpiMatrix, 1, 0, "AVAILABLE CREDIT", "Rs. 0", ThemeColors.Success);
            _lblTotalPurchases = CreateKpiCard(kpiMatrix, 2, 0, "TOTAL CHARGES", "Rs. 0", ThemeColors.Primary);

            _lblTotalPaid = CreateKpiCard(kpiMatrix, 0, 1, "TOTAL RECOVERED", "Rs. 0", ThemeColors.Success);
            _lblOutstanding = CreateKpiCard(kpiMatrix, 1, 1, "CURRENT BALANCE", "Rs. 0", ThemeColors.Danger);
            _lblOverdue = CreateKpiCard(kpiMatrix, 2, 1, "OVERDUE ARREARS", "Rs. 0", Color.FromArgb(185, 28, 28));

            pnlSummary.Controls.Add(kpiMatrix, 1, 0);
            Controls.Add(pnlSummary);

            // 4. Tab Control for Plans & Ledger
            var pnlTabCard = UIHelper.CreateCardPanel(new Padding(6));
            pnlTabCard.Dock = DockStyle.Fill;
            pnlTabCard.Margin = new Padding(0, 10, 0, 0);

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.2f)
            };

            // Tab 1: Installment Plans & Agreements
            var tabPlans = new TabPage { Text = "  🛒 Installment Plans & Deals  ", BackColor = Color.White };
            _gridPlans = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridPlans.Columns.Add("Invoice", "Invoice / Agreement");
            _gridPlans.Columns.Add("Date", "Date");
            _gridPlans.Columns.Add("Type", "Deal Type");
            _gridPlans.Columns.Add("Details", "Products / Units");
            _gridPlans.Columns.Add("Total", "Net Amount");
            _gridPlans.Columns.Add("Paid", "Total Paid");
            _gridPlans.Columns.Add("Balance", "Remaining Balance");
            _gridPlans.Columns.Add("Status", "Status");
            tabPlans.Controls.Add(_gridPlans);
            _tabControl.TabPages.Add(tabPlans);

            // Tab 2: Universal Customer Ledger
            var tabLedger = new TabPage { Text = "  📜 Full Ledger & Statement of Account  ", BackColor = Color.White };
            _gridLedger = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridLedger.Columns.Add("Date", "Date");
            _gridLedger.Columns.Add("Trx", "Transaction #");
            _gridLedger.Columns.Add("Type", "Type");
            _gridLedger.Columns.Add("Desc", "Description");
            _gridLedger.Columns.Add("Debit", "Debit (Charges)");
            _gridLedger.Columns.Add("Credit", "Credit (Payments)");
            _gridLedger.Columns.Add("Balance", "Balance");
            tabLedger.Controls.Add(_gridLedger);
            _tabControl.TabPages.Add(tabLedger);

            pnlTabCard.Controls.Add(_tabControl);
            Controls.Add(pnlTabCard);

            // Z-Order
            pnlTabCard.SendToBack();
            pnlHeader.BringToFront();
        }

        private Label CreateKpiCard(TableLayoutPanel pnl, int col, int row, string title, string initialVal, Color valColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(3),
                Padding = new Padding(8, 6, 8, 6)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemeColors.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 7.2f, FontStyle.Bold),
                ForeColor = ThemeColors.TextMuted,
                Dock = DockStyle.Top,
                Height = 16
            };
            var lblV = new Label
            {
                Text = initialVal,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = valColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblV);
            card.Controls.Add(lblT);
            pnl.Controls.Add(card, col, row);

            return lblV;
        }

        public void LoadCustomers()
        {
            var customers = TenantService.GetAllTenants(TenantStatus.Active);
            _cboCustomerPicker.DisplayMember = "DisplayName";
            _cboCustomerPicker.ValueMember = "Id";

            var list = customers.Select(c => new
            {
                c.Id,
                DisplayName = $"{c.FullName} ({c.TenantCode}) - {c.ContactNumber}"
            }).ToList();

            _cboCustomerPicker.DataSource = list;

            if (_selectedCustomerId.HasValue && _selectedCustomerId.Value > 0)
            {
                _cboCustomerPicker.SelectedValue = _selectedCustomerId.Value;
                SelectCustomer(_selectedCustomerId.Value);
            }
            else if (list.Any())
            {
                SelectCustomer(list.First().Id);
            }
        }

        public void SelectCustomer(int customerId)
        {
            _selectedCustomerId = customerId;
            var profile = _receivablesService.GetCustomerCreditProfile(customerId);
            if (profile == null) return;

            _lblCustomerName.Text = $"{profile.FullName} ({profile.CustomerCode}) - {profile.CustomerType}";
            _lblPhone.Text = $"{profile.ContactNumber} | CNIC: {profile.CnicOrId ?? "-"}";
            _lblAddress.Text = $"{profile.PermanentAddress ?? "-"} | {profile.City ?? "-"}";
            _lblGuarantor.Text = !string.IsNullOrWhiteSpace(profile.GuarantorName) ? $"{profile.GuarantorName} ({profile.GuarantorPhone}) [{profile.GuarantorRelation}]" : "None";
            _lblRating.Text = $"Risk Rating: {profile.Rating}";
            _lblRating.ForeColor = profile.Rating == "Risky" || profile.Rating == "Defaulter" ? ThemeColors.Danger : ThemeColors.Success;

            _lblCreditLimit.Text = profile.CreditLimit > 0 ? SettingService.FormatCurrency(profile.CreditLimit) : "No Limit";
            _lblAvailCredit.Text = profile.CreditLimit > 0 ? SettingService.FormatCurrency(profile.AvailableCredit) : "Unlimited";
            _lblTotalPurchases.Text = SettingService.FormatCurrency(profile.TotalPurchases);
            _lblTotalPaid.Text = SettingService.FormatCurrency(profile.TotalPaid);
            _lblOutstanding.Text = SettingService.FormatCurrency(profile.CurrentOutstanding);
            _lblOverdue.Text = SettingService.FormatCurrency(profile.OverdueAmount);

            // Load Plans Grid
            _gridPlans.Rows.Clear();
            var sales = _saleService.GetInstallmentSales(customerId: customerId);
            foreach (var s in sales)
            {
                string itemsStr = string.Join(", ", s.Items.Select(i => i.ItemDescription));
                _gridPlans.Rows.Add(
                    s.InvoiceNumber,
                    s.SaleDate.ToString("dd/MM/yyyy"),
                    s.SaleType.ToString(),
                    itemsStr,
                    SettingService.FormatCurrency(s.NetSalePrice),
                    SettingService.FormatCurrency(s.TotalPaid),
                    SettingService.FormatCurrency(s.RemainingBalance),
                    s.Status.ToString()
                );
            }

            // Load Ledger
            _gridLedger.Rows.Clear();
            var stmt = LedgerService.GetTenantLedger(customerId);
            foreach (var e in stmt.Entries)
            {
                _gridLedger.Rows.Add(
                    e.Date.ToString("dd/MM/yyyy"),
                    e.TransactionCode,
                    e.TypeName,
                    e.Description,
                    SettingService.FormatCurrency(e.Debit),
                    SettingService.FormatCurrency(e.Credit),
                    SettingService.FormatCurrency(e.Balance)
                );
            }
        }

        private void BtnStatement_Click(object? sender, EventArgs e)
        {
            if (_selectedCustomerId.HasValue && _selectedCustomerId.Value > 0)
            {
                var ds = ReportService.GetTenantStatementReport(_selectedCustomerId.Value);
                PrintingService.PrintReport(ds, showPreview: true);
            }
        }

        private void BtnNewSale_Click(object? sender, EventArgs e)
        {
            using var dlg = new NewInstallmentSaleForm(_saleService, _productService, _currentUserId, _currentUsername, _selectedCustomerId);
            if (dlg.ShowDialog() == DialogResult.OK && _selectedCustomerId.HasValue)
            {
                SelectCustomer(_selectedCustomerId.Value);
            }
        }
    }
}
