using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class InstallmentPaymentDialog : Form
    {
        private readonly InstallmentSaleService _saleService;
        private readonly InstallmentSale _sale;
        private readonly int _currentUserId;
        private readonly string _currentUsername;

        private Label _lblCustomerInfo = null!;
        private Label _lblInvoiceInfo = null!;
        private Label _lblBalance = null!;
        private ComboBox _cboSchedulePicker = null!;
        private NumericUpDown _numPaymentAmount = null!;
        private ComboBox _cboPaymentMethod = null!;
        private TextBox _txtReference = null!;
        private TextBox _txtRemarks = null!;
        private CheckBox _chkEarlySettlement = null!;
        private NumericUpDown _numDiscount = null!;
        private Label _lblDiscountTag = null!;
        private ModernButton _btnSaveAndPrint = null!;
        private ModernButton _btnSaveOnly = null!;
        private ModernButton _btnCancel = null!;

        public PaymentReceipt? GeneratedReceipt { get; private set; }

        public InstallmentPaymentDialog(
            InstallmentSaleService saleService,
            InstallmentSale sale,
            int currentUserId,
            string currentUsername)
        {
            _saleService = saleService;
            _sale = sale;
            _currentUserId = currentUserId;
            _currentUsername = currentUsername;

            InitializeComponent();
            LoadSaleData();
        }

        private void InitializeComponent()
        {
            Text = $"Collect Installment Payment - {_sale.InvoiceNumber}";
            Width = 620;
            Height = 570;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "💳 Collect Installment Payment",
                $"Invoice: {_sale.InvoiceNumber} | Customer: {_sale.Customer?.FullName ?? "N/A"}"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 9,
                Padding = new Padding(0)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            for (int i = 0; i < 9; i++)
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            // Customer
            _lblCustomerInfo = new Label { Font = ThemeColors.SubHeadingFont, ForeColor = ThemeColors.TextPrimary, AutoSize = true, Anchor = AnchorStyles.Left };
            mainLayout.Controls.Add(new Label { Text = "Customer:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary }, 0, 0);
            mainLayout.Controls.Add(_lblCustomerInfo, 1, 0);

            // Invoice / Products
            _lblInvoiceInfo = new Label { AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            mainLayout.Controls.Add(new Label { Text = "Invoice / Items:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary }, 0, 1);
            mainLayout.Controls.Add(_lblInvoiceInfo, 1, 1);

            // Outstanding Balance
            _lblBalance = new Label { Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = ThemeColors.Danger, AutoSize = true, Anchor = AnchorStyles.Left };
            mainLayout.Controls.Add(new Label { Text = "Remaining Balance:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextSecondary }, 0, 2);
            mainLayout.Controls.Add(_lblBalance, 1, 2);

            // Target Schedule
            _cboSchedulePicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320, Height = 28 };
            _cboSchedulePicker.SelectedIndexChanged += CboSchedulePicker_SelectedIndexChanged;
            mainLayout.Controls.Add(new Label { Text = "Apply Towards:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 3);
            mainLayout.Controls.Add(_cboSchedulePicker, 1, 3);

            // Payment Amount
            _numPaymentAmount = new NumericUpDown { Maximum = 100000000, DecimalPlaces = 2, Width = 180, Height = 28, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
            mainLayout.Controls.Add(new Label { Text = "Payment Amount *:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 4);
            mainLayout.Controls.Add(_numPaymentAmount, 1, 4);

            // Early Settlement & Discount
            _chkEarlySettlement = new CheckBox { Text = "Early Settlement", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary };
            _chkEarlySettlement.CheckedChanged += ChkEarlySettlement_CheckedChanged;
            _numDiscount = new NumericUpDown { Maximum = 10000000, DecimalPlaces = 2, Value = 0, Width = 110, Height = 26, Visible = false };
            _lblDiscountTag = new Label { Text = "Discount:", AutoSize = true, Visible = false, Margin = new Padding(5, 4, 0, 0), ForeColor = ThemeColors.TextPrimary };

            var earlyFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            earlyFlow.Controls.Add(_chkEarlySettlement);
            earlyFlow.Controls.Add(_lblDiscountTag);
            earlyFlow.Controls.Add(_numDiscount);

            mainLayout.Controls.Add(new Label { Text = "Settlement Option:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 5);
            mainLayout.Controls.Add(earlyFlow, 1, 5);

            // Payment Method
            _cboPaymentMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Height = 28 };
            _cboPaymentMethod.Items.Add(PaymentMethod.Cash);
            _cboPaymentMethod.Items.Add(PaymentMethod.BankTransfer);
            _cboPaymentMethod.Items.Add(PaymentMethod.OnlineTransfer);
            _cboPaymentMethod.Items.Add(PaymentMethod.Cheque);
            _cboPaymentMethod.Items.Add(PaymentMethod.Other);
            _cboPaymentMethod.SelectedItem = PaymentMethod.Cash;
            mainLayout.Controls.Add(new Label { Text = "Payment Method:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 6);
            mainLayout.Controls.Add(_cboPaymentMethod, 1, 6);

            // Reference
            _txtReference = new TextBox { Width = 320, Height = 28, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Cheque #, Slip #, Transaction ID" };
            mainLayout.Controls.Add(new Label { Text = "Reference / Slip #:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 7);
            mainLayout.Controls.Add(_txtReference, 1, 7);

            // Remarks
            _txtRemarks = new TextBox { Width = 320, Height = 28, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Optional payment remarks" };
            mainLayout.Controls.Add(new Label { Text = "Remarks:", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = ThemeColors.TextPrimary }, 0, 8);
            mainLayout.Controls.Add(_txtRemarks, 1, 8);

            pnlCard.Controls.Add(mainLayout);

            // Buttons
            _btnSaveAndPrint = new ModernButton
            {
                Text = "✓ Collect & Print",
                StyleType = ButtonStyleType.Primary,
                Width = 160,
                Height = 38
            };
            _btnSaveAndPrint.Click += (s, e) => SavePayment(true);

            _btnSaveOnly = new ModernButton
            {
                Text = "Collect Only",
                StyleType = ButtonStyleType.Success,
                Width = 120,
                Height = 38
            };
            _btnSaveOnly.Click += (s, e) => SavePayment(false);

            _btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            _btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            var pnlDialogFooter = UIHelper.CreateDialogFooter(_btnSaveAndPrint, _btnSaveOnly, _btnCancel);

            var containerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
            containerPanel.Controls.Add(pnlCard);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private void LoadSaleData()
        {
            _lblCustomerInfo.Text = $"{_sale.Customer?.FullName} ({_sale.Customer?.TenantCode}) - {_sale.Customer?.ContactNumber}";
            string itemsStr = string.Join(", ", _sale.Items.Select(i => i.ItemDescription));
            _lblInvoiceInfo.Text = $"{_sale.InvoiceNumber} - {itemsStr}";
            _lblBalance.Text = SettingService.FormatCurrency(_sale.RemainingBalance);

            _cboSchedulePicker.DisplayMember = "DisplayName";
            _cboSchedulePicker.ValueMember = "Id";

            var schList = new System.Collections.ArrayList
            {
                new { Id = 0, DisplayName = "-- Auto-Allocate to Oldest Due --", Amount = _sale.InstallmentAmount }
            };

            foreach (var sch in _sale.Schedules.OrderBy(s => s.InstallmentNumber))
            {
                string status = sch.Status == InstallmentItemStatus.Paid ? "PAID" : $"Due: {sch.DueDate:dd/MM/yyyy} (Rem: {SettingService.FormatCurrency(sch.RemainingAmount)})";
                schList.Add(new
                {
                    sch.Id,
                    DisplayName = $"Installment #{sch.InstallmentNumber} - {status}",
                    Amount = sch.RemainingAmount > 0 ? sch.RemainingAmount : sch.DueAmount
                });
            }

            _cboSchedulePicker.DataSource = schList;

            // Default payment amount to first pending schedule remaining amount or standard installment
            var firstPending = _sale.Schedules.FirstOrDefault(s => s.Status != InstallmentItemStatus.Paid && s.RemainingAmount > 0);
            _numPaymentAmount.Value = firstPending != null ? firstPending.RemainingAmount : (_sale.RemainingBalance > 0 ? _sale.RemainingBalance : 0);
        }

        private void CboSchedulePicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cboSchedulePicker.SelectedItem != null && !_chkEarlySettlement.Checked)
            {
                dynamic item = _cboSchedulePicker.SelectedItem;
                if (item.Id > 0 && item.Amount > 0)
                {
                    _numPaymentAmount.Value = Math.Min((decimal)item.Amount, _sale.RemainingBalance);
                }
            }
        }

        private void ChkEarlySettlement_CheckedChanged(object? sender, EventArgs e)
        {
            bool early = _chkEarlySettlement.Checked;
            _lblDiscountTag.Visible = early;
            _numDiscount.Visible = early;

            if (early)
            {
                _cboSchedulePicker.Enabled = false;
                _numPaymentAmount.Value = _sale.RemainingBalance;
            }
            else
            {
                _cboSchedulePicker.Enabled = true;
                _numDiscount.Value = 0;
            }
        }

        private void SavePayment(bool printReceipt)
        {
            decimal amount = _numPaymentAmount.Value;
            if (amount <= 0 && !_chkEarlySettlement.Checked)
            {
                ModernMessageBox.ShowWarning("Please enter a valid payment amount.", "Validation", this);
                return;
            }

            var method = (PaymentMethod)(_cboPaymentMethod.SelectedItem ?? PaymentMethod.Cash);
            string? refNum = string.IsNullOrWhiteSpace(_txtReference.Text) ? null : _txtReference.Text.Trim();
            string? remarks = string.IsNullOrWhiteSpace(_txtRemarks.Text) ? null : _txtRemarks.Text.Trim();

            if (_chkEarlySettlement.Checked)
            {
                decimal disc = _numDiscount.Value;
                var res = _saleService.EarlySettleSale(_sale.Id, amount, disc, method, remarks, _currentUserId, _currentUsername);
                if (!res.Success)
                {
                    ModernMessageBox.ShowWarning(res.Message, "Settlement Error", this);
                    return;
                }
                ModernMessageBox.ShowInfo(res.Message, "Sale Settled", this);
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            int? targetSchId = (_cboSchedulePicker.SelectedValue is int sid && sid > 0) ? sid : null;
            var result = _saleService.CollectInstallmentPayment(
                _sale.Id,
                amount,
                method,
                refNum,
                remarks,
                _currentUserId,
                _currentUsername,
                targetSchId
            );

            if (!result.Success)
            {
                ModernMessageBox.ShowWarning(result.Message, "Payment Collection Error", this);
                return;
            }

            GeneratedReceipt = result.Receipt;

            if (printReceipt && GeneratedReceipt != null)
            {
                PrintingService.PrintReceipt(GeneratedReceipt, false, true);
            }

            ModernMessageBox.ShowInfo(result.Message, "Payment Successful", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
