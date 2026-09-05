using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class ProductManagementView : UserControl
    {
        private readonly ProductService _productService;
        private readonly InventoryService _inventoryService;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private TextBox _txtSearch = null!;
        private ComboBox _cboCategory = null!;
        private CheckBox _chkLowStockOnly = null!;
        private ModernButton _btnAddProduct = null!;
        private ModernButton _btnStockIn = null!;
        private ModernButton _btnAdjustStock = null!;
        private ModernButton _btnEdit = null!;
        private ModernButton _btnDelete = null!;
        private ModernDataGridView _grid = null!;

        private StatCardControl _cardTotalProducts = null!;
        private StatCardControl _cardTotalUnits = null!;
        private StatCardControl _cardValuation = null!;
        private StatCardControl _cardLowStock = null!;

        public ProductManagementView(
            ProductService productService,
            InventoryService inventoryService,
            int currentUserId,
            string currentUsername)
        {
            _productService = productService;
            _inventoryService = inventoryService;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;

            InitializeComponent();
            LoadCategories();
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
                "📦 Product Catalog & Stock Inventory Management",
                "Maintain complete product catalog, cash/installment pricing, serial/IMEI tracking, stock replenishment, and automated valuation."
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
                PlaceholderText = "Search by Name, Code, Barcode, Brand, Serial...",
                Width = 260,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 8, 0)
            };
            _txtSearch.TextChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_txtSearch);

            _cboCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 140,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 3, 8, 0)
            };
            _cboCategory.SelectedIndexChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_cboCategory);

            _chkLowStockOnly = new CheckBox
            {
                Text = "Low Stock Only",
                AutoSize = true,
                Margin = new Padding(4, 7, 10, 0),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ThemeColors.Danger
            };
            _chkLowStockOnly.CheckedChanged += (s, e) => RefreshData();
            topFlow.Controls.Add(_chkLowStockOnly);

            _btnAddProduct = new ModernButton
            {
                Text = "+ Add Product",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(125, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnAddProduct.Click += BtnAddProduct_Click;
            topFlow.Controls.Add(_btnAddProduct);

            _btnStockIn = new ModernButton
            {
                Text = "📦 Stock In",
                StyleType = ButtonStyleType.Success,
                Size = new Size(110, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnStockIn.Click += BtnStockIn_Click;
            topFlow.Controls.Add(_btnStockIn);

            _btnAdjustStock = new ModernButton
            {
                Text = "⚖ Adjust Stock",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(115, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnAdjustStock.Click += BtnAdjustStock_Click;
            topFlow.Controls.Add(_btnAdjustStock);

            _btnEdit = new ModernButton
            {
                Text = "Edit",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(70, 34),
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnEdit.Click += BtnEdit_Click;
            topFlow.Controls.Add(_btnEdit);

            _btnDelete = new ModernButton
            {
                Text = "Delete",
                StyleType = ButtonStyleType.Danger,
                Size = new Size(80, 34),
                Margin = new Padding(0, 0, 0, 0)
            };
            _btnDelete.Click += BtnDelete_Click;
            topFlow.Controls.Add(_btnDelete);

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
            _grid.Columns.Add("Id", "ID");
            _grid.Columns["Id"].Visible = false;
            _grid.Columns.Add("Code", "Product Code");
            _grid.Columns.Add("Name", "Product Name");
            _grid.Columns.Add("Category", "Category");
            _grid.Columns.Add("BrandModel", "Brand / Model");
            _grid.Columns.Add("Barcode", "Barcode / Serial");
            _grid.Columns.Add("Cost", "Cost Price");
            _grid.Columns.Add("CashPrice", "Cash Price");
            _grid.Columns.Add("InstallmentPrice", "Installment Price");
            _grid.Columns.Add("Stock", "Stock Qty");
            _grid.Columns.Add("Status", "Status");

            _grid.CellDoubleClick += (s, e) => BtnEdit_Click(s, e);
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

            _cardTotalProducts = new StatCardControl
            {
                Title = "Total Products",
                Value = "0",
                Subtitle = "Catalog SKUs",
                IconSymbol = "📦",
                AccentColor = ThemeColors.Primary,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalProducts);

            _cardTotalUnits = new StatCardControl
            {
                Title = "Units In Stock",
                Value = "0",
                Subtitle = "Physical Inventory",
                IconSymbol = "📊",
                AccentColor = ThemeColors.Info,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardTotalUnits);

            _cardValuation = new StatCardControl
            {
                Title = "Inventory Valuation",
                Value = "Rs. 0",
                Subtitle = "Cost Valuation",
                IconSymbol = "💰",
                AccentColor = ThemeColors.Success,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 12, 0)
            };
            bottomFlow.Controls.Add(_cardValuation);

            _cardLowStock = new StatCardControl
            {
                Title = "Low Stock Alerts",
                Value = "0",
                Subtitle = "Requires Restock",
                IconSymbol = "⚠️",
                AccentColor = ThemeColors.Danger,
                Width = 240,
                Height = 80,
                Margin = new Padding(0, 0, 0, 0)
            };
            bottomFlow.Controls.Add(_cardLowStock);

            Controls.Add(bottomFlow);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();
        }

        private void LoadCategories()
        {
            _cboCategory.Items.Clear();
            _cboCategory.Items.Add("All Categories");
            foreach (var c in _productService.GetCategories())
            {
                _cboCategory.Items.Add(c);
            }
            _cboCategory.SelectedIndex = 0;
        }

        public void RefreshData()
        {
            string? search = string.IsNullOrWhiteSpace(_txtSearch.Text) ? null : _txtSearch.Text.Trim();
            string? cat = _cboCategory.SelectedItem?.ToString() == "All Categories" ? null : _cboCategory.SelectedItem?.ToString();

            var products = _productService.GetAllProducts(search, cat, activeOnly: true);

            if (_chkLowStockOnly.Checked)
            {
                products = products.FindAll(p => p.TrackStock && p.CurrentStock <= p.MinimumStockLevel);
            }

            _grid.Rows.Clear();
            foreach (var p in products)
            {
                string status = !p.TrackStock ? "Non-tracked" : (p.CurrentStock == 0 ? "Out of Stock" : (p.CurrentStock <= p.MinimumStockLevel ? "Low Stock" : "In Stock"));
                _grid.Rows.Add(
                    p.Id,
                    p.ProductCode,
                    p.Name,
                    p.Category,
                    $"{p.Brand} {p.Model}".Trim(),
                    p.Barcode ?? p.SerialNumber ?? "-",
                    SettingService.FormatCurrency(p.PurchasePrice),
                    SettingService.FormatCurrency(p.CashSalePrice),
                    SettingService.FormatCurrency(p.InstallmentSalePrice),
                    $"{p.CurrentStock} {p.Unit}",
                    status
                );
            }

            var summary = _inventoryService.GetStockSummary();
            _cardTotalProducts.Value = summary.TotalProducts.ToString();
            _cardTotalUnits.Value = summary.TotalUnitsInStock.ToString("N0");
            _cardValuation.Value = SettingService.FormatCurrency(summary.TotalStockValuation);
            _cardLowStock.Value = summary.LowStockCount.ToString();
        }

        private Product? GetSelectedProduct()
        {
            if (_grid.CurrentRow != null && _grid.CurrentRow.Cells["Id"].Value is int id)
            {
                return _productService.GetProductById(id);
            }
            return null;
        }

        private void BtnAddProduct_Click(object? sender, EventArgs e)
        {
            using var dlg = new ProductEditForm(_productService, _currentUserId, _currentUsername);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                ModernMessageBox.ShowInfo("Please select a product to edit.", "Selection Required", this);
                return;
            }

            using var dlg = new ProductEditForm(_productService, _currentUserId, _currentUsername, p);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnStockIn_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                ModernMessageBox.ShowInfo("Please select a product for stock entry / purchase.", "Selection Required", this);
                return;
            }

            using var dlg = new StockAdjustmentForm(_inventoryService, p, _currentUserId, _currentUsername, StockMovementType.Purchase);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnAdjustStock_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null)
            {
                ModernMessageBox.ShowInfo("Please select a product to adjust stock.", "Selection Required", this);
                return;
            }

            using var dlg = new StockAdjustmentForm(_inventoryService, p, _currentUserId, _currentUsername, StockMovementType.StockAdjustment);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                RefreshData();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var p = GetSelectedProduct();
            if (p == null) return;

            if (ModernMessageBox.ShowConfirm($"Are you sure you want to delete product '{p.Name}' ({p.ProductCode})?", "Confirm Delete", this))
            {
                var result = _productService.DeleteProduct(p.Id, _currentUserId, _currentUsername);
                ModernMessageBox.ShowInfo(result.Message, "Product Action", this);
                RefreshData();
            }
        }
    }
}
