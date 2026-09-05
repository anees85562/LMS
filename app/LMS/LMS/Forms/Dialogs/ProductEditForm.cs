using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class ProductEditForm : Form
    {
        private readonly ProductService _productService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;
        private readonly Product _product;

        private TextBox _txtCode = null!;
        private TextBox _txtBarcode = null!;
        private TextBox _txtName = null!;
        private ComboBox _cboCategory = null!;
        private TextBox _txtBrand = null!;
        private TextBox _txtModel = null!;
        private TextBox _txtSerialNumber = null!;
        private NumericUpDown _numPurchasePrice = null!;
        private NumericUpDown _numCashPrice = null!;
        private NumericUpDown _numInstallmentPrice = null!;
        private NumericUpDown _numOpeningStock = null!;
        private NumericUpDown _numMinStock = null!;
        private TextBox _txtUnit = null!;
        private TextBox _txtWarranty = null!;
        private CheckBox _chkTrackStock = null!;
        private CheckBox _chkIsActive = null!;
        private TextBox _txtNotes = null!;
        private ModernButton _btnSave = null!;
        private ModernButton _btnCancel = null!;

        public Product? SavedProduct { get; private set; }

        public ProductEditForm(ProductService productService, int currentUserId, string currentUsername, Product? product = null)
        {
            _productService = productService;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;
            _product = product ?? new Product
            {
                ProductCode = _productService.GenerateNextProductCode(),
                TrackStock = true,
                IsActive = true,
                CurrentStock = 0,
                MinimumStockLevel = 2,
                Unit = "Pcs"
            };

            InitializeComponent();
            LoadProductData();
        }

        private void InitializeComponent()
        {
            Text = _product.Id == 0 ? "Add New Product / Item" : "Edit Product";
            Width = 720;
            Height = 670;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                _product.Id == 0 ? "📦 Add New Product / Inventory Item" : "📦 Edit Product Details",
                "Configure pricing, brand/model, serial tracking, barcode, and inventory stock alert levels"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 9,
                Padding = new Padding(0),
                AutoScroll = true
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));

            for (int i = 0; i < 9; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            // Product Code & Barcode
            _txtCode = CreateStyledTextBox();
            _txtBarcode = CreateStyledTextBox();
            AddRow(mainPanel, 0, "Product Code *", _txtCode, "Barcode / UPC", _txtBarcode);

            // Name
            _txtName = CreateStyledTextBox();
            var lblName = new Label { Text = "Product Name *", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
            mainPanel.Controls.Add(lblName, 0, 1);
            mainPanel.Controls.Add(_txtName, 1, 1);
            mainPanel.SetColumnSpan(_txtName, 3);

            // Category & Unit
            _cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill, Height = 28 };
            foreach (var cat in _productService.GetCategories())
            {
                _cboCategory.Items.Add(cat);
            }
            if (_cboCategory.Items.Count > 0) _cboCategory.SelectedIndex = 0;

            _txtUnit = CreateStyledTextBox();
            _txtUnit.Text = "Pcs";
            AddRow(mainPanel, 2, "Category", _cboCategory, "Unit (Pcs/Box)", _txtUnit);

            // Brand & Model
            _txtBrand = CreateStyledTextBox();
            _txtModel = CreateStyledTextBox();
            AddRow(mainPanel, 3, "Brand / Make", _txtBrand, "Model", _txtModel);

            // Serial # & Warranty
            _txtSerialNumber = CreateStyledTextBox();
            _txtWarranty = CreateStyledTextBox();
            _txtWarranty.PlaceholderText = "e.g. 1 Year Official";
            AddRow(mainPanel, 4, "Serial / IMEI #", _txtSerialNumber, "Warranty", _txtWarranty);

            // Purchase Price & Cash Price
            _numPurchasePrice = CreateStyledNumeric(0, 100000000, 2);
            _numCashPrice = CreateStyledNumeric(0, 100000000, 2);
            AddRow(mainPanel, 5, "Purchase Cost", _numPurchasePrice, "Cash Sale Price", _numCashPrice);

            // Installment Price & Opening Stock
            _numInstallmentPrice = CreateStyledNumeric(0, 100000000, 2);
            _numOpeningStock = CreateStyledNumeric(0, 100000, 0);
            AddRow(mainPanel, 6, "Installment Price", _numInstallmentPrice, _product.Id == 0 ? "Opening Stock" : "Current Stock", _numOpeningStock);

            // Min Stock & Track Stock
            _numMinStock = CreateStyledNumeric(0, 10000, 0);
            _numMinStock.Value = 2;
            _chkTrackStock = new CheckBox { Text = "Track Inventory", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            AddRow(mainPanel, 7, "Min Alert Stock", _numMinStock, "", _chkTrackStock);

            // Active & Notes
            _chkIsActive = new CheckBox { Text = "Active Product", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            _txtNotes = CreateStyledTextBox();
            _txtNotes.PlaceholderText = "Optional notes";
            AddRow(mainPanel, 8, "Status", _chkIsActive, "Notes", _txtNotes);

            pnlCard.Controls.Add(mainPanel);

            // Buttons panel
            _btnSave = new ModernButton
            {
                Text = "✓ Save Product",
                StyleType = ButtonStyleType.Primary,
                Width = 140,
                Height = 38
            };
            _btnSave.Click += BtnSave_Click;

            _btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            var pnlDialogFooter = UIHelper.CreateDialogFooter(_btnSave, _btnCancel);

            var containerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
            containerPanel.Controls.Add(pnlCard);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private TextBox CreateStyledTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle,
                Font = ThemeColors.BodyFont
            };
        }

        private NumericUpDown CreateStyledNumeric(decimal min, decimal max, int decimals)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Dock = DockStyle.Fill,
                Height = 28,
                Font = ThemeColors.BodyFont
            };
        }

        private void AddRow(TableLayoutPanel pnl, int row, string lbl1, Control ctrl1, string lbl2, Control ctrl2)
        {
            if (!string.IsNullOrEmpty(lbl1))
            {
                pnl.Controls.Add(new Label { Text = lbl1, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            }
            pnl.Controls.Add(ctrl1, 1, row);

            if (!string.IsNullOrEmpty(lbl2))
            {
                pnl.Controls.Add(new Label { Text = lbl2, AutoSize = true, Anchor = AnchorStyles.Left }, 2, row);
            }
            pnl.Controls.Add(ctrl2, 3, row);
        }

        private void LoadProductData()
        {
            _txtCode.Text = _product.ProductCode;
            _txtBarcode.Text = _product.Barcode ?? "";
            _txtName.Text = _product.Name;
            _cboCategory.Text = _product.Category;
            _txtBrand.Text = _product.Brand ?? "";
            _txtModel.Text = _product.Model ?? "";
            _txtSerialNumber.Text = _product.SerialNumber ?? "";
            _numPurchasePrice.Value = _product.PurchasePrice;
            _numCashPrice.Value = _product.CashSalePrice;
            _numInstallmentPrice.Value = _product.InstallmentSalePrice > 0 ? _product.InstallmentSalePrice : _product.CashSalePrice;
            _numOpeningStock.Value = _product.CurrentStock;
            _numMinStock.Value = _product.MinimumStockLevel;
            _txtUnit.Text = _product.Unit;
            _txtWarranty.Text = _product.Warranty ?? "";
            _chkTrackStock.Checked = _product.TrackStock;
            _chkIsActive.Checked = _product.IsActive;
            _txtNotes.Text = _product.Notes ?? "";

            if (_product.Id > 0)
            {
                _numOpeningStock.Enabled = false; // For existing product, use Stock Adjustment form to change stock
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                ModernMessageBox.ShowWarning("Please enter the Product Name.", "Validation Error", this);
                _txtName.Focus();
                return;
            }

            _product.ProductCode = _txtCode.Text.Trim();
            _product.Barcode = string.IsNullOrWhiteSpace(_txtBarcode.Text) ? null : _txtBarcode.Text.Trim();
            _product.Name = _txtName.Text.Trim();
            _product.Category = string.IsNullOrWhiteSpace(_cboCategory.Text) ? "General" : _cboCategory.Text.Trim();
            _product.Brand = string.IsNullOrWhiteSpace(_txtBrand.Text) ? null : _txtBrand.Text.Trim();
            _product.Model = string.IsNullOrWhiteSpace(_txtModel.Text) ? null : _txtModel.Text.Trim();
            _product.SerialNumber = string.IsNullOrWhiteSpace(_txtSerialNumber.Text) ? null : _txtSerialNumber.Text.Trim();
            _product.PurchasePrice = _numPurchasePrice.Value;
            _product.CashSalePrice = _numCashPrice.Value;
            _product.InstallmentSalePrice = _numInstallmentPrice.Value;
            if (_product.Id == 0)
            {
                _product.CurrentStock = (int)_numOpeningStock.Value;
            }
            _product.MinimumStockLevel = (int)_numMinStock.Value;
            _product.Unit = string.IsNullOrWhiteSpace(_txtUnit.Text) ? "Pcs" : _txtUnit.Text.Trim();
            _product.Warranty = string.IsNullOrWhiteSpace(_txtWarranty.Text) ? null : _txtWarranty.Text.Trim();
            _product.TrackStock = _chkTrackStock.Checked;
            _product.IsActive = _chkIsActive.Checked;
            _product.Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim();

            var result = _productService.SaveProduct(_product, _currentUserId, _currentUsername);
            if (!result.Success)
            {
                ModernMessageBox.ShowError(result.Message, "Error Saving Product", this);
                return;
            }

            SavedProduct = result.Product;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
