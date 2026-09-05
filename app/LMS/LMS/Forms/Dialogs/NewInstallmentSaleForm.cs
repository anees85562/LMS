using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class NewInstallmentSaleForm : Form
    {
        private readonly InstallmentSaleService _saleService;
        private readonly ProductService _productService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private ComboBox _cboCustomer = null!;
        private ComboBox _cboSaleType = null!;
        private DateTimePicker _dtpSaleDate = null!;
        private TextBox _txtInvoiceNo = null!;

        // Items Table
        private ModernDataGridView _gridItems = null!;
        private ComboBox _cboProductPicker = null!;
        private TextBox _txtItemDesc = null!;
        private TextBox _txtSerialNo = null!;
        private NumericUpDown _numItemQty = null!;
        private NumericUpDown _numItemPrice = null!;
        private ModernButton _btnAddItem = null!;
        private ModernButton _btnRemoveItem = null!;
        private readonly List<SaleItem> _saleItems = new();

        // Financial Plan Controls
        private Label _lblTotalGross = null!;
        private NumericUpDown _numDiscount = null!;
        private Label _lblNetTotal = null!;
        private NumericUpDown _numDownPayment = null!;
        private Label _lblFinancedAmount = null!;
        private NumericUpDown _numInstallments = null!;
        private ComboBox _cboFrequency = null!;
        private DateTimePicker _dtpFirstDueDate = null!;
        private Label _lblMonthlyInstallment = null!;

        // Schedule Preview Grid
        private ModernDataGridView _gridSchedule = null!;

        // Guarantor info
        private TextBox _txtGuarantorName = null!;
        private TextBox _txtGuarantorPhone = null!;
        private TextBox _txtGuarantorCnic = null!;
        private TextBox _txtGuarantorAddress = null!;
        private TextBox _txtTerms = null!;

        // Action buttons
        private ModernButton _btnSaveAndPrint = null!;
        private ModernButton _btnSaveOnly = null!;
        private ModernButton _btnCancel = null!;

        public InstallmentSale? CreatedSale { get; private set; }

        public NewInstallmentSaleForm(
            InstallmentSaleService saleService,
            ProductService productService,
            int currentUserId,
            string currentUsername,
            int? preselectedCustomerId = null)
        {
            _saleService = saleService;
            _productService = productService;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;

            InitializeComponent();
            LoadCustomers(preselectedCustomerId);
            LoadProducts();
            RecalculateTotals();
        }

        private void InitializeComponent()
        {
            Text = "New Installment / BNPL / Credit Sale Invoice";
            Width = 1040;
            Height = 820;
            MinimumSize = new Size(950, 750);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "🛒 New Installment / BNPL / Credit Sale",
                "Generate customer installment account, configure payment plan schedule, and record initial down payment"
            );

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(16, 10, 16, 10)
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));  // Customer & Invoice Details
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));   // Products & Items
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));   // Financial Plan & Schedule Preview
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 18));   // Guarantor Info

            // --- 1. Customer & Sale Bar ---
            var grpCustomer = UIHelper.CreateCardPanel(new Padding(10, 6, 10, 6));
            grpCustomer.Dock = DockStyle.Fill;
            var headerFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };

            headerFlow.Controls.Add(new Label { Text = "Customer *:", AutoSize = true, Margin = new Padding(0, 6, 5, 0), Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.TextPrimary });
            _cboCustomer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, Height = 28 };
            _cboCustomer.SelectedIndexChanged += (s, e) => AutoFillGuarantorFromCustomer();
            headerFlow.Controls.Add(_cboCustomer);

            headerFlow.Controls.Add(new Label { Text = "Sale Type:", AutoSize = true, Margin = new Padding(12, 6, 5, 0), Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.TextPrimary });
            _cboSaleType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 28 };
            _cboSaleType.Items.Add(SaleType.InstallmentSale);
            _cboSaleType.Items.Add(SaleType.BNPLSale);
            _cboSaleType.Items.Add(SaleType.CreditSale);
            _cboSaleType.Items.Add(SaleType.CashSale);
            _cboSaleType.SelectedItem = SaleType.InstallmentSale;
            _cboSaleType.SelectedIndexChanged += (s, e) => RecalculateTotals();
            headerFlow.Controls.Add(_cboSaleType);

            headerFlow.Controls.Add(new Label { Text = "Sale Date:", AutoSize = true, Margin = new Padding(12, 6, 5, 0), Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.TextPrimary });
            _dtpSaleDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Height = 28 };
            headerFlow.Controls.Add(_dtpSaleDate);

            headerFlow.Controls.Add(new Label { Text = "Invoice #:", AutoSize = true, Margin = new Padding(12, 6, 5, 0), Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.TextPrimary });
            _txtInvoiceNo = new TextBox { Text = _saleService.GenerateNextInvoiceNumber(), Width = 120, Height = 28, BorderStyle = BorderStyle.FixedSingle };
            headerFlow.Controls.Add(_txtInvoiceNo);

            grpCustomer.Controls.Add(headerFlow);
            mainLayout.Controls.Add(grpCustomer, 0, 0);

            // --- 2. Items Section ---
            var grpItems = UIHelper.CreateCardPanel(new Padding(10));
            grpItems.Dock = DockStyle.Fill;

            var itemsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            itemsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            itemsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var itemInputFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            _cboProductPicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210, Height = 28 };
            _cboProductPicker.SelectedIndexChanged += CboProductPicker_SelectedIndexChanged;
            itemInputFlow.Controls.Add(new Label { Text = "Product:", AutoSize = true, Margin = new Padding(0, 6, 2, 0), ForeColor = ThemeColors.TextPrimary });
            itemInputFlow.Controls.Add(_cboProductPicker);

            _txtItemDesc = new TextBox { Width = 180, PlaceholderText = "Description", Height = 28, BorderStyle = BorderStyle.FixedSingle };
            itemInputFlow.Controls.Add(_txtItemDesc);

            _txtSerialNo = new TextBox { Width = 120, PlaceholderText = "Serial / IMEI #", Height = 28, BorderStyle = BorderStyle.FixedSingle };
            itemInputFlow.Controls.Add(_txtSerialNo);

            itemInputFlow.Controls.Add(new Label { Text = "Qty:", AutoSize = true, Margin = new Padding(5, 6, 2, 0), ForeColor = ThemeColors.TextPrimary });
            _numItemQty = new NumericUpDown { Maximum = 1000, Minimum = 1, Value = 1, Width = 55, Height = 28 };
            itemInputFlow.Controls.Add(_numItemQty);

            itemInputFlow.Controls.Add(new Label { Text = "Price:", AutoSize = true, Margin = new Padding(5, 6, 2, 0), ForeColor = ThemeColors.TextPrimary });
            _numItemPrice = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, Width = 100, Height = 28 };
            itemInputFlow.Controls.Add(_numItemPrice);

            _btnAddItem = new ModernButton
            {
                Text = "+ Add Item",
                StyleType = ButtonStyleType.Primary,
                Width = 95,
                Height = 28
            };
            _btnAddItem.Click += BtnAddItem_Click;
            itemInputFlow.Controls.Add(_btnAddItem);

            _btnRemoveItem = new ModernButton
            {
                Text = "Remove",
                StyleType = ButtonStyleType.Danger,
                Width = 75,
                Height = 28
            };
            _btnRemoveItem.Click += BtnRemoveItem_Click;
            itemInputFlow.Controls.Add(_btnRemoveItem);

            itemsLayout.Controls.Add(itemInputFlow, 0, 0);

            _gridItems = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridItems.Columns.Add("Desc", "Item Description");
            _gridItems.Columns.Add("Serial", "Serial / Model");
            _gridItems.Columns.Add("Qty", "Qty");
            _gridItems.Columns.Add("UnitPrice", "Unit Price");
            _gridItems.Columns.Add("Total", "Total Price");

            itemsLayout.Controls.Add(_gridItems, 0, 1);
            grpItems.Controls.Add(itemsLayout);
            mainLayout.Controls.Add(grpItems, 0, 1);

            // --- 3. Financial Plan & Schedule Section ---
            var grpPlan = UIHelper.CreateCardPanel(new Padding(10));
            grpPlan.Dock = DockStyle.Fill;

            var planSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            planSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            planSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Left Plan Inputs
            var planInputs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7
            };
            planInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            planInputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            for (int i = 0; i < 7; i++) planInputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

            _lblTotalGross = new Label { Text = "Rs. 0.00", Font = ThemeColors.SubHeadingFont, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            planInputs.Controls.Add(new Label { Text = "Gross Total:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 0);
            planInputs.Controls.Add(_lblTotalGross, 1, 0);

            _numDiscount = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, Width = 120, Height = 26 };
            _numDiscount.ValueChanged += (s, e) => RecalculateTotals();
            planInputs.Controls.Add(new Label { Text = "Discount:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 1);
            planInputs.Controls.Add(_numDiscount, 1, 1);

            _lblNetTotal = new Label { Text = "Rs. 0.00", Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.Primary, AutoSize = true, Anchor = AnchorStyles.Left };
            planInputs.Controls.Add(new Label { Text = "Net Price:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 2);
            planInputs.Controls.Add(_lblNetTotal, 1, 2);

            _numDownPayment = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, Width = 120, Height = 26 };
            _numDownPayment.ValueChanged += (s, e) => RecalculateTotals();
            planInputs.Controls.Add(new Label { Text = "Down Payment *:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 3);
            planInputs.Controls.Add(_numDownPayment, 1, 3);

            _lblFinancedAmount = new Label { Text = "Rs. 0.00", Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.Danger, AutoSize = true, Anchor = AnchorStyles.Left };
            planInputs.Controls.Add(new Label { Text = "Remaining / Financed:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 4);
            planInputs.Controls.Add(_lblFinancedAmount, 1, 4);

            var instFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _numInstallments = new NumericUpDown { Maximum = 120, Minimum = 1, Value = 10, Width = 55, Height = 26 };
            _numInstallments.ValueChanged += (s, e) => RecalculateTotals();
            _cboFrequency = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95, Height = 26 };
            _cboFrequency.Items.Add(InstallmentFrequency.Monthly);
            _cboFrequency.Items.Add(InstallmentFrequency.Weekly);
            _cboFrequency.Items.Add(InstallmentFrequency.BiWeekly);
            _cboFrequency.SelectedItem = InstallmentFrequency.Monthly;
            _cboFrequency.SelectedIndexChanged += (s, e) => RecalculateTotals();
            instFlow.Controls.Add(_numInstallments);
            instFlow.Controls.Add(_cboFrequency);

            planInputs.Controls.Add(new Label { Text = "Installments / Freq:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 5);
            planInputs.Controls.Add(instFlow, 1, 5);

            var dueFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _dtpFirstDueDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(1), Width = 100, Height = 26 };
            _dtpFirstDueDate.ValueChanged += (s, e) => RecalculateTotals();
            _lblMonthlyInstallment = new Label { Text = "Inst: Rs. 0.00", Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.Success, AutoSize = true, Margin = new Padding(5, 3, 0, 0) };
            dueFlow.Controls.Add(_dtpFirstDueDate);
            dueFlow.Controls.Add(_lblMonthlyInstallment);

            planInputs.Controls.Add(new Label { Text = "First Due Date:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 6);
            planInputs.Controls.Add(dueFlow, 1, 6);

            planSplit.Controls.Add(planInputs, 0, 0);

            // Right Schedule Preview Grid
            _gridSchedule = new ModernDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridSchedule.Columns.Add("No", "#");
            _gridSchedule.Columns.Add("DueDate", "Due Date");
            _gridSchedule.Columns.Add("Amount", "Due Amount");

            planSplit.Controls.Add(_gridSchedule, 1, 0);
            grpPlan.Controls.Add(planSplit);
            mainLayout.Controls.Add(grpPlan, 0, 2);

            // --- 4. Guarantor & Terms ---
            var grpGuarantor = UIHelper.CreateCardPanel(new Padding(10, 6, 10, 6));
            grpGuarantor.Dock = DockStyle.Fill;

            var guarTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2
            };
            guarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            guarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            guarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            guarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

            _txtGuarantorName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Height = 26 };
            _txtGuarantorPhone = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Height = 26 };
            _txtGuarantorCnic = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Height = 26 };
            _txtGuarantorAddress = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Height = 26 };

            guarTable.Controls.Add(new Label { Text = "Guarantor Name:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 0);
            guarTable.Controls.Add(_txtGuarantorName, 1, 0);
            guarTable.Controls.Add(new Label { Text = "Guarantor Phone:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 2, 0);
            guarTable.Controls.Add(_txtGuarantorPhone, 3, 0);

            guarTable.Controls.Add(new Label { Text = "Guarantor CNIC:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 1);
            guarTable.Controls.Add(_txtGuarantorCnic, 1, 1);
            guarTable.Controls.Add(new Label { Text = "Guarantor Address:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 2, 1);
            guarTable.Controls.Add(_txtGuarantorAddress, 3, 1);

            _txtTerms = new TextBox
            {
                Text = "Goods sold on installment remain property of merchant until full payment is received. Late payments subject to penalty.",
                Visible = false
            };

            grpGuarantor.Controls.Add(guarTable);
            mainLayout.Controls.Add(grpGuarantor, 0, 3);

            // --- 5. Footer Buttons ---
            _btnSaveAndPrint = new ModernButton
            {
                Text = "✓ Create Sale & Print Invoice",
                StyleType = ButtonStyleType.Primary,
                Width = 220,
                Height = 38
            };
            _btnSaveAndPrint.Click += (s, e) => SaveSale(true);

            _btnSaveOnly = new ModernButton
            {
                Text = "Create Sale Only",
                StyleType = ButtonStyleType.Success,
                Width = 140,
                Height = 38
            };
            _btnSaveOnly.Click += (s, e) => SaveSale(false);

            _btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            var pnlDialogFooter = UIHelper.CreateDialogFooter(_btnSaveAndPrint, _btnSaveOnly, _btnCancel);

            Controls.Add(mainLayout);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private void LoadCustomers(int? preselectedId = null)
        {
            var customers = TenantService.GetAllTenants(TenantStatus.Active);
            _cboCustomer.DisplayMember = "DisplayName";
            _cboCustomer.ValueMember = "Id";

            var list = customers.Select(c => new
            {
                c.Id,
                DisplayName = $"{c.FullName} ({c.TenantCode}) - {c.ContactNumber}"
            }).ToList();

            _cboCustomer.DataSource = list;

            if (preselectedId.HasValue && preselectedId.Value > 0)
            {
                _cboCustomer.SelectedValue = preselectedId.Value;
            }
        }

        private void LoadProducts()
        {
            var products = _productService.GetAllProducts(activeOnly: true);
            _cboProductPicker.DisplayMember = "DisplayName";
            _cboProductPicker.ValueMember = "Id";

            var list = new List<object>
            {
                new { Id = 0, DisplayName = "-- Select or Custom Item --" }
            };

            foreach (var p in products)
            {
                list.Add(new
                {
                    p.Id,
                    DisplayName = $"{p.Name} ({p.ProductCode}) - Price: {p.CashSalePrice:N0}"
                });
            }

            _cboProductPicker.DataSource = list;
        }

        private void CboProductPicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cboProductPicker.SelectedValue is int prodId && prodId > 0)
            {
                var prod = _productService.GetProductById(prodId);
                if (prod != null)
                {
                    _txtItemDesc.Text = $"{prod.Name} ({prod.Brand} {prod.Model})".Trim();
                    _txtSerialNo.Text = prod.SerialNumber ?? "";
                    var saleType = (SaleType)(_cboSaleType.SelectedItem ?? SaleType.InstallmentSale);
                    _numItemPrice.Value = (saleType == SaleType.InstallmentSale && prod.InstallmentSalePrice > 0)
                        ? prod.InstallmentSalePrice
                        : prod.CashSalePrice;
                }
            }
        }

        private void AutoFillGuarantorFromCustomer()
        {
            if (_cboCustomer.SelectedValue is int custId && custId > 0)
            {
                var cust = TenantService.GetTenantById(custId);
                if (cust != null && !string.IsNullOrWhiteSpace(cust.GuarantorName))
                {
                    _txtGuarantorName.Text = cust.GuarantorName;
                    _txtGuarantorPhone.Text = cust.GuarantorPhone ?? "";
                    _txtGuarantorCnic.Text = cust.GuarantorCnic ?? "";
                    _txtGuarantorAddress.Text = cust.GuarantorAddress ?? "";
                }
            }
        }

        private void BtnAddItem_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtItemDesc.Text))
            {
                ModernMessageBox.ShowWarning("Please enter an item description.", "Validation", this);
                return;
            }

            int? prodId = (_cboProductPicker.SelectedValue is int pId && pId > 0) ? pId : null;
            int qty = (int)_numItemQty.Value;
            decimal unitPrice = _numItemPrice.Value;
            decimal total = qty * unitPrice;

            var item = new SaleItem
            {
                ProductId = prodId,
                ItemDescription = _txtItemDesc.Text.Trim(),
                SerialNumber = string.IsNullOrWhiteSpace(_txtSerialNo.Text) ? null : _txtSerialNo.Text.Trim(),
                Quantity = qty,
                UnitPrice = unitPrice,
                InstallmentPrice = unitPrice,
                TotalPrice = total
            };

            _saleItems.Add(item);
            RefreshItemsGrid();
            RecalculateTotals();

            // Clear inputs
            _cboProductPicker.SelectedIndex = 0;
            _txtItemDesc.Clear();
            _txtSerialNo.Clear();
            _numItemQty.Value = 1;
            _numItemPrice.Value = 0;
        }

        private void BtnRemoveItem_Click(object? sender, EventArgs e)
        {
            if (_gridItems.CurrentRow != null && _gridItems.CurrentRow.Index >= 0)
            {
                int idx = _gridItems.CurrentRow.Index;
                if (idx < _saleItems.Count)
                {
                    _saleItems.RemoveAt(idx);
                    RefreshItemsGrid();
                    RecalculateTotals();
                }
            }
        }

        private void RefreshItemsGrid()
        {
            _gridItems.Rows.Clear();
            foreach (var i in _saleItems)
            {
                _gridItems.Rows.Add(i.ItemDescription, i.SerialNumber ?? "-", i.Quantity, SettingService.FormatCurrency(i.UnitPrice), SettingService.FormatCurrency(i.TotalPrice));
            }
        }

        private void RecalculateTotals()
        {
            decimal gross = _saleItems.Sum(i => i.TotalPrice);
            _lblTotalGross.Text = SettingService.FormatCurrency(gross);

            decimal discount = _numDiscount.Value;
            decimal net = Math.Max(0, gross - discount);
            _lblNetTotal.Text = SettingService.FormatCurrency(net);

            var saleType = (SaleType)(_cboSaleType.SelectedItem ?? SaleType.InstallmentSale);
            if (saleType == SaleType.CashSale)
            {
                _numDownPayment.Value = net;
                _numInstallments.Value = 1;
                _numInstallments.Enabled = false;
                _cboFrequency.Enabled = false;
            }
            else
            {
                _numInstallments.Enabled = true;
                _cboFrequency.Enabled = true;
            }

            decimal downPayment = _numDownPayment.Value;
            decimal financed = Math.Max(0, net - downPayment);
            _lblFinancedAmount.Text = SettingService.FormatCurrency(financed);

            int n = (int)_numInstallments.Value;
            var freq = (InstallmentFrequency)(_cboFrequency.SelectedItem ?? InstallmentFrequency.Monthly);
            DateTime firstDue = _dtpFirstDueDate.Value;

            decimal monthlyInst = (n > 0 && financed > 0) ? Math.Round(financed / n, 2) : 0;
            _lblMonthlyInstallment.Text = $"Inst: {SettingService.FormatCurrency(monthlyInst)}";

            // Preview schedule
            _gridSchedule.Rows.Clear();
            if (n > 0 && financed > 0)
            {
                var schedules = _saleService.GenerateScheduleList(financed, n, freq, firstDue);
                foreach (var s in schedules)
                {
                    _gridSchedule.Rows.Add($"#{s.InstallmentNumber}", s.DueDate.ToString("dd/MM/yyyy"), SettingService.FormatCurrency(s.DueAmount));
                }
            }
        }

        private void SaveSale(bool printInvoice)
        {
            if (_cboCustomer.SelectedValue is not int custId || custId <= 0)
            {
                ModernMessageBox.ShowWarning("Please select a customer.", "Validation", this);
                return;
            }

            if (!_saleItems.Any())
            {
                ModernMessageBox.ShowWarning("Please add at least one item or product to the sale.", "Validation", this);
                return;
            }

            decimal gross = _saleItems.Sum(i => i.TotalPrice);
            decimal discount = _numDiscount.Value;
            decimal net = Math.Max(0, gross - discount);
            decimal downPayment = _numDownPayment.Value;
            var saleType = (SaleType)(_cboSaleType.SelectedItem ?? SaleType.InstallmentSale);
            int n = (int)_numInstallments.Value;
            var freq = (InstallmentFrequency)(_cboFrequency.SelectedItem ?? InstallmentFrequency.Monthly);

            var sale = new InstallmentSale
            {
                InvoiceNumber = _txtInvoiceNo.Text.Trim(),
                CustomerId = custId,
                SaleType = saleType,
                SaleDate = _dtpSaleDate.Value,
                TotalCashPrice = gross,
                TotalInstallmentPrice = gross,
                Discount = discount,
                NetSalePrice = net,
                DownPayment = downPayment,
                NumberOfInstallments = n,
                Frequency = freq,
                InstallmentAmount = (n > 0 && net - downPayment > 0) ? Math.Round((net - downPayment) / n, 2) : 0,
                FirstDueDate = _dtpFirstDueDate.Value,
                GuarantorName = string.IsNullOrWhiteSpace(_txtGuarantorName.Text) ? null : _txtGuarantorName.Text.Trim(),
                GuarantorPhone = string.IsNullOrWhiteSpace(_txtGuarantorPhone.Text) ? null : _txtGuarantorPhone.Text.Trim(),
                GuarantorCnic = string.IsNullOrWhiteSpace(_txtGuarantorCnic.Text) ? null : _txtGuarantorCnic.Text.Trim(),
                GuarantorAddress = string.IsNullOrWhiteSpace(_txtGuarantorAddress.Text) ? null : _txtGuarantorAddress.Text.Trim(),
                TermsAndConditions = _txtTerms.Text
            };

            var result = _saleService.CreateInstallmentSale(sale, _saleItems, _currentUserId, _currentUsername);
            if (!result.Success)
            {
                ModernMessageBox.ShowError(result.Message, "Error Creating Sale", this);
                return;
            }

            CreatedSale = _saleService.GetInstallmentSaleById(result.Sale!.Id);

            if (printInvoice && CreatedSale != null)
            {
                PrintingService.PrintInvoice(CreatedSale, false, true);
            }

            ModernMessageBox.ShowInfo(result.Message, "Sale Created", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
