using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Dialogs
{
    public class RecordPaymentForm : Form
    {
        private int? _preselectedTenantId;
        private ComboBox cmbTenant = null!;
        private Label lblCurrentBalance = null!;
        private Label lblUnitInfo = null!;
        private DateTimePicker dtpDate = null!;
        private TextBox txtAmount = null!;
        private ComboBox cmbMethod = null!;
        private TextBox txtRef = null!;
        private TextBox txtBank = null!;
        private TextBox txtPeriod = null!;
        private TextBox txtRemarks = null!;
        private CheckBox chkPrintReceipt = null!;
        private ModernButton btnSave = null!;
        private ModernButton btnCancel = null!;

        private List<Tenant> _tenants = new();

        public RecordPaymentForm(int? tenantId = null, decimal? defaultAmount = null, string? rentalPeriod = null)
        {
            _preselectedTenantId = tenantId;
            InitializeComponent();

            if (defaultAmount.HasValue && defaultAmount.Value > 0)
            {
                txtAmount.Text = defaultAmount.Value.ToString("N0");
            }

            if (!string.IsNullOrWhiteSpace(rentalPeriod))
            {
                txtPeriod.Text = rentalPeriod;
            }
        }

        private void InitializeComponent()
        {
            Text = "Record Rent & Account Payment";
            Size = new Size(620, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "💳 Record Rent & Customer Payment",
                "Receive payment, update customer account balance, record ledger entry, and issue official receipt"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(0)
            };

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Tenant
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // Info box
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Date
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Amount
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Method
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Ref & Bank
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Period
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Remarks & Checkbox

            // Tenant
            mainPanel.Controls.Add(CreateFieldLabel("Select Tenant / Customer *"), 0, 0);
            cmbTenant = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Height = 28 };
            cmbTenant.SelectedIndexChanged += CmbTenant_SelectedIndexChanged;
            mainPanel.Controls.Add(cmbTenant, 1, 0);

            // Balance & Unit Info Box
            var infoBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeColors.PrimaryLight,
                Padding = new Padding(8, 4, 8, 4)
            };
            infoBox.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(191, 219, 254)), 0, 0, infoBox.Width - 1, infoBox.Height - 1);

            lblCurrentBalance = new Label
            {
                Text = "Current Outstanding: Rs. 0",
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.PrimaryDark,
                Dock = DockStyle.Top,
                Height = 22
            };
            lblUnitInfo = new Label
            {
                Text = "Unit: None",
                Font = ThemeColors.SmallFont,
                ForeColor = ThemeColors.TextSecondary,
                Dock = DockStyle.Top,
                Height = 20
            };
            infoBox.Controls.Add(lblUnitInfo);
            infoBox.Controls.Add(lblCurrentBalance);
            mainPanel.Controls.Add(new Label { Text = "Account Status:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextSecondary }, 0, 1);
            mainPanel.Controls.Add(infoBox, 1, 1);

            // Date
            mainPanel.Controls.Add(CreateFieldLabel("Payment Date *"), 0, 2);
            dtpDate = new DateTimePicker { Dock = DockStyle.Left, Width = 180, Height = 28, Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            mainPanel.Controls.Add(dtpDate, 1, 2);

            // Payment Amount
            mainPanel.Controls.Add(CreateFieldLabel("Amount Paid (Rs.) *"), 0, 3);
            txtAmount = new TextBox { Dock = DockStyle.Left, Width = 180, Height = 28, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = ThemeColors.Success };
            mainPanel.Controls.Add(txtAmount, 1, 3);

            // Payment Method
            mainPanel.Controls.Add(CreateFieldLabel("Payment Method"), 0, 4);
            cmbMethod = new ComboBox { Dock = DockStyle.Left, Width = 180, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMethod.Items.AddRange(new object[] { PaymentMethod.Cash, PaymentMethod.BankTransfer, PaymentMethod.Cheque, PaymentMethod.OnlineTransfer, PaymentMethod.Other });
            cmbMethod.SelectedItem = PaymentMethod.Cash;
            cmbMethod.SelectedIndexChanged += (s, e) =>
            {
                bool isBank = (PaymentMethod)cmbMethod.SelectedItem != PaymentMethod.Cash;
                txtRef.Enabled = isBank;
                txtBank.Enabled = isBank;
            };
            mainPanel.Controls.Add(cmbMethod, 1, 4);

            // Ref & Bank Name
            mainPanel.Controls.Add(CreateFieldLabel("Cheque / Bank Ref #"), 0, 5);
            var refFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            txtRef = new TextBox { Width = 150, Height = 28, BorderStyle = BorderStyle.FixedSingle, Enabled = false, PlaceholderText = "Cheque/Slip #" };
            txtBank = new TextBox { Width = 160, Height = 28, BorderStyle = BorderStyle.FixedSingle, Enabled = false, PlaceholderText = "Bank Name" };
            refFlow.Controls.Add(txtRef);
            refFlow.Controls.Add(txtBank);
            mainPanel.Controls.Add(refFlow, 1, 5);

            // Rental Period
            mainPanel.Controls.Add(CreateFieldLabel("Rental / Target Period"), 0, 6);
            txtPeriod = new TextBox { Text = DateTime.Now.ToString("MMMM yyyy"), Dock = DockStyle.Fill, Height = 28, BorderStyle = BorderStyle.FixedSingle };
            mainPanel.Controls.Add(txtPeriod, 1, 6);

            // Remarks & Checkbox
            mainPanel.Controls.Add(CreateFieldLabel("Remarks / Receipt"), 0, 7);
            var remarksLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0) };
            remarksLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            remarksLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            txtRemarks = new TextBox { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Optional payment remarks" };
            chkPrintReceipt = new CheckBox
            {
                Text = "Print / Preview Receipt Immediately",
                Dock = DockStyle.Fill,
                Checked = true,
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.Primary,
                Cursor = Cursors.Hand
            };
            remarksLayout.Controls.Add(txtRemarks, 0, 0);
            remarksLayout.Controls.Add(chkPrintReceipt, 0, 1);
            mainPanel.Controls.Add(remarksLayout, 1, 7);

            pnlCard.Controls.Add(mainPanel);

            btnSave = new ModernButton
            {
                Text = "✓ Save & Process Payment",
                StyleType = ButtonStyleType.Success,
                Width = 200,
                Height = 38
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var pnlDialogFooter = UIHelper.CreateDialogFooter(btnSave, btnCancel);

            var containerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
            containerPanel.Controls.Add(pnlCard);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);

            Load += RecordPaymentForm_Load;
        }

        private Label CreateFieldLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ThemeColors.TextPrimary, Font = ThemeColors.BodyFont };
        }

        private void RecordPaymentForm_Load(object? sender, EventArgs e)
        {
            _tenants = TenantService.GetAllTenants();
            cmbTenant.DisplayMember = "FullName";
            cmbTenant.ValueMember = "Id";
            cmbTenant.DataSource = _tenants;

            if (_preselectedTenantId.HasValue && _preselectedTenantId.Value > 0)
            {
                cmbTenant.SelectedValue = _preselectedTenantId.Value;
            }
        }

        private void CmbTenant_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbTenant.SelectedValue is int tenantId && tenantId > 0)
            {
                decimal bal = TenantService.GetTenantCurrentBalance(tenantId);
                lblCurrentBalance.Text = $"Current Outstanding: {SettingService.FormatCurrency(bal)}";
                lblCurrentBalance.ForeColor = bal > 0 ? ThemeColors.Danger : (bal < 0 ? ThemeColors.Primary : ThemeColors.Success);

                var tenant = _tenants.FirstOrDefault(t => t.Id == tenantId);
                var activeLease = tenant?.RentAgreements.FirstOrDefault(a => a.Status == AgreementStatus.Active);
                if (activeLease != null && activeLease.PropertyUnit != null)
                {
                    lblUnitInfo.Text = $"Unit: {activeLease.PropertyUnit.Property?.Name} - {activeLease.PropertyUnit.UnitNumber} (Rent: {SettingService.FormatCurrency(activeLease.MonthlyRent)})";
                    if (string.IsNullOrWhiteSpace(txtAmount.Text) || txtAmount.Text == "0")
                    {
                        txtAmount.Text = (bal > 0 ? bal : activeLease.MonthlyRent).ToString("N0");
                    }
                }
                else
                {
                    lblUnitInfo.Text = "No active unit lease found";
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (cmbTenant.SelectedValue is not int tenantId || tenantId <= 0)
            {
                ModernMessageBox.ShowWarning("Please select a tenant.", "Validation", this);
                return;
            }

            decimal.TryParse(txtAmount.Text.Replace(",", ""), out decimal amount);
            if (amount <= 0)
            {
                ModernMessageBox.ShowWarning("Please enter a valid payment amount greater than zero.", "Validation", this);
                return;
            }

            var dto = new PaymentDto
            {
                TenantId = tenantId,
                Amount = amount,
                PaymentDate = dtpDate.Value,
                PaymentMethod = (PaymentMethod)(cmbMethod.SelectedItem ?? PaymentMethod.Cash),
                ReferenceNumber = txtRef.Text.Trim(),
                BankName = txtBank.Text.Trim(),
                RentalPeriod = txtPeriod.Text.Trim(),
                Remarks = txtRemarks.Text.Trim()
            };

            var res = PaymentService.RecordPayment(dto);
            if (!res.Success)
            {
                ModernMessageBox.ShowError(res.Message, "Error", this);
                return;
            }

            if (chkPrintReceipt.Checked && res.Receipt != null)
            {
                // Instant Print Preview
                PrintingService.PrintReceipt(res.Receipt, isReprint: false, showPreview: true);
            }
            else
            {
                ModernMessageBox.ShowInfo($"Payment of {SettingService.FormatCurrency(amount)} recorded successfully!\nReceipt Number: {res.Receipt?.ReceiptNumber}", "Payment Success", this);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public class VoidTransactionForm : Form
    {
        private int _transactionId;
        private string _txInfo;
        private TextBox txtReason = null!;
        private ModernButton btnConfirm = null!;
        private ModernButton btnCancel = null!;

        public VoidTransactionForm(int transactionId, string txInfo)
        {
            _transactionId = transactionId;
            _txInfo = txInfo;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Void / Reverse Transaction";
            Size = new Size(540, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeColors.CanvasBg;
            Font = ThemeColors.BodyFont;

            var pnlDialogHeader = UIHelper.CreateDialogHeader(
                "⚠️ Void / Reverse Transaction Entry",
                "Create an offsetting reversal entry in double-entry ledger preserving complete audit log"
            );

            var pnlCard = UIHelper.CreateCardPanel(new Padding(16, 12, 16, 12));
            pnlCard.Dock = DockStyle.Fill;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblWarning = new Label
            {
                Text = "⚠️ You are about to void the following transaction. It will NOT be deleted, but marked as voided and an offsetting reversal entry will be posted to preserve the financial audit trail.",
                Dock = DockStyle.Fill,
                ForeColor = ThemeColors.Danger,
                Font = ThemeColors.SmallFont
            };
            mainLayout.Controls.Add(lblWarning, 0, 0);

            var lblTx = new Label
            {
                Text = $"Transaction: {_txInfo}",
                Font = ThemeColors.SubHeadingFont,
                ForeColor = ThemeColors.TextPrimary,
                Dock = DockStyle.Fill
            };
            mainLayout.Controls.Add(lblTx, 0, 1);

            var reasonPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0) };
            reasonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            reasonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var lblReason = new Label
            {
                Text = "Reason for Voiding *:",
                Dock = DockStyle.Fill,
                ForeColor = ThemeColors.TextPrimary,
                Font = ThemeColors.SubHeadingFont
            };
            reasonPanel.Controls.Add(lblReason, 0, 0);

            txtReason = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "e.g. Incorrect amount entered, payment made by check that bounced, wrong tenant selected"
            };
            reasonPanel.Controls.Add(txtReason, 0, 1);

            mainLayout.Controls.Add(reasonPanel, 0, 2);
            pnlCard.Controls.Add(mainLayout);

            btnConfirm = new ModernButton
            {
                Text = "⚠️ Confirm Void",
                StyleType = ButtonStyleType.Danger,
                Width = 140,
                Height = 38
            };
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new ModernButton
            {
                Text = "Cancel",
                StyleType = ButtonStyleType.Secondary,
                Width = 90,
                Height = 38
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var pnlDialogFooter = UIHelper.CreateDialogFooter(btnConfirm, btnCancel);

            var containerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12) };
            containerPanel.Controls.Add(pnlCard);

            Controls.Add(containerPanel);
            Controls.Add(pnlDialogFooter);
            Controls.Add(pnlDialogHeader);
        }

        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                ModernMessageBox.ShowWarning("Please enter a reason for voiding this transaction.", "Validation", this);
                return;
            }

            var res = PaymentService.VoidTransaction(_transactionId, txtReason.Text.Trim());
            if (!res.Success)
            {
                ModernMessageBox.ShowError(res.Message, "Error", this);
                return;
            }

            ModernMessageBox.ShowInfo(res.Message, "Voided", this);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
