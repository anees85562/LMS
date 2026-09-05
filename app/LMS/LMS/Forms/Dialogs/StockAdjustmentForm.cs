using System;
using System.Drawing;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class StockAdjustmentForm : Form
    {
        private readonly InventoryService _inventoryService;
        private readonly Product _product;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private ComboBox _cboMovementType = null!;
        private NumericUpDown _numQuantity = null!;
        private NumericUpDown _numUnitPrice = null!;
        private TextBox _txtReference = null!;
        private TextBox _txtRemarks = null!;
        private Label _lblCurrentStock = null!;
        private Label _lblExpectedNewStock = null!;
        private ModernButton _btnSave = null!;
        private ModernButton _btnCancel = null!;

        public StockAdjustmentForm(
            InventoryService inventoryService,
            Product product,
            int currentUserId,
            string currentUsername,
            StockMovementType defaultType = StockMovementType.Purchase)
        {
            _inventoryService = inventoryService;
            _product = product;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;

            InitializeComponent();
            _cboMovementType.SelectedItem = defaultType;
            UpdateStockPreview();
        }

        private void InitializeComponent()
        {
            Text = $"Manage Inventory - {_product.Name} ({_product.ProductCode})";
            Width = 580;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "📊 Inventory Movement / Stock Adjustment",
                $"{_product.Name} (Code: {_product.ProductCode} | In Stock: {_product.CurrentStock} {_product.Unit})"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(0)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            for (int i = 0; i < 7; i++)
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            // Current Stock display
            _lblCurrentStock = new Label
            {
                Text = $"{_product.CurrentStock} {_product.Unit}",
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.Primary,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            mainPanel.Controls.Add(new Label { Text = "Current Stock:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary }, 0, 0);
            mainPanel.Controls.Add(_lblCurrentStock, 1, 0);

            // Operation / Movement Type
            _cboMovementType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Height = 28 };
            _cboMovementType.Items.Add(StockMovementType.Purchase);
            _cboMovementType.Items.Add(StockMovementType.StockAdjustment);
            _cboMovementType.Items.Add(StockMovementType.DamagedStock);
            _cboMovementType.Items.Add(StockMovementType.Return);
            _cboMovementType.SelectedIndexChanged += (s, e) => UpdateStockPreview();

            mainPanel.Controls.Add(new Label { Text = "Operation Type:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 1);
            mainPanel.Controls.Add(_cboMovementType, 1, 1);

            // Quantity
            _numQuantity = new NumericUpDown { Maximum = 100000, Minimum = 1, Value = 1, Width = 150, Height = 28 };
            _numQuantity.ValueChanged += (s, e) => UpdateStockPreview();
            mainPanel.Controls.Add(new Label { Text = "Quantity / New Count:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 2);
            mainPanel.Controls.Add(_numQuantity, 1, 2);

            // Unit Price / Cost
            _numUnitPrice = new NumericUpDown { Maximum = 10000000, DecimalPlaces = 2, Value = _product.PurchasePrice, Width = 150, Height = 28 };
            mainPanel.Controls.Add(new Label { Text = "Unit Cost / Price:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 3);
            mainPanel.Controls.Add(_numUnitPrice, 1, 3);

            // Reference
            _txtReference = new TextBox { Width = 300, Height = 28, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. Invoice #, Bill #, Batch #" };
            mainPanel.Controls.Add(new Label { Text = "Reference #:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 4);
            mainPanel.Controls.Add(_txtReference, 1, 4);

            // Remarks / Reason
            _txtRemarks = new TextBox { Width = 300, Height = 28, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Reason for adjustment or purchase notes" };
            mainPanel.Controls.Add(new Label { Text = "Remarks / Reason:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 5);
            mainPanel.Controls.Add(_txtRemarks, 1, 5);

            // Result Preview
            _lblExpectedNewStock = new Label
            {
                Text = "New Stock: -",
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.Success,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };
            mainPanel.Controls.Add(new Label { Text = "Calculated Stock:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary }, 0, 6);
            mainPanel.Controls.Add(_lblExpectedNewStock, 1, 6);

            pnlCard.Controls.Add(mainPanel);

            // Buttons
            _btnSave = new ModernButton
            {
                Text = "✓ Confirm Movement",
                StyleType = ButtonStyleType.Primary,
                Width = 170,
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

        private void UpdateStockPreview()
        {
            if (_cboMovementType.SelectedItem == null) return;
            var type = (StockMovementType)_cboMovementType.SelectedItem;
            int qty = (int)_numQuantity.Value;

            int newStock = type switch
            {
                StockMovementType.Purchase => _product.CurrentStock + qty,
                StockMovementType.Return => _product.CurrentStock + qty,
                StockMovementType.DamagedStock => Math.Max(0, _product.CurrentStock - qty),
                StockMovementType.StockAdjustment => qty,
                _ => _product.CurrentStock
            };

            _lblExpectedNewStock.Text = $"Will change from {_product.CurrentStock} to {newStock} {_product.Unit}";
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_cboMovementType.SelectedItem == null) return;
            var type = (StockMovementType)_cboMovementType.SelectedItem;
            int qty = (int)_numQuantity.Value;
            decimal price = _numUnitPrice.Value;
            string? refNum = string.IsNullOrWhiteSpace(_txtReference.Text) ? null : _txtReference.Text.Trim();
            string? remarks = string.IsNullOrWhiteSpace(_txtRemarks.Text) ? null : _txtRemarks.Text.Trim();

            (bool Success, string Message) result = type switch
            {
                StockMovementType.Purchase => _inventoryService.RecordPurchase(_product.Id, qty, price, refNum, remarks, _currentUserId, _currentUsername),
                StockMovementType.StockAdjustment => _inventoryService.RecordStockAdjustment(_product.Id, qty, remarks ?? "Manual audit adjustment", _currentUserId, _currentUsername),
                StockMovementType.DamagedStock => _inventoryService.RecordDamagedStock(_product.Id, qty, remarks ?? "Damaged stock", _currentUserId, _currentUsername),
                StockMovementType.Return => _inventoryService.RecordReturn(_product.Id, qty, refNum ?? "RET-MANUAL", remarks, _currentUserId, _currentUsername),
                _ => (false, "Unsupported movement type.")
            };

            if (!result.Success)
            {
                ModernMessageBox.ShowWarning(result.Message, "Inventory Error", this);
                return;
            }

            ModernMessageBox.ShowInfo(result.Message, "Inventory Updated", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
