using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class DataImportExportView : UserControl
    {
        private TextBox txtTenantCsvPath = null!;
        private TextBox txtPropCsvPath = null!;
        private TextBox txtLog = null!;

        public DataImportExportView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);
            AutoScroll = true;

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                "📥 Bulk Data Migration & CSV Import Wizard",
                "Easily import existing tenant spreadsheets and property listings with automated validation, schema mapping, and safety snapshots."
            );
            Controls.Add(pnlHeader);

            var mainCard = UIHelper.CreateCardPanel(new Padding(20));
            mainCard.Dock = DockStyle.Top;
            mainCard.AutoSize = true;
            mainCard.Margin = new Padding(0, 8, 0, 20);

            int y = 10;

            // SECTION 1: Sample Templates
            var lblSec1 = new Label { Text = "1. Download Sample CSV Templates", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.Primary, Location = new Point(10, y), AutoSize = true };
            mainCard.Controls.Add(lblSec1);
            y += 28;

            var lblTmplHelp = new Label { Text = "Use these pre-formatted templates to organize your tenant and property records in Excel before importing:", Location = new Point(10, y), AutoSize = true, ForeColor = ThemeColors.TextSecondary };
            mainCard.Controls.Add(lblTmplHelp);
            y += 26;

            var btnDownloadTenTmpl = new ModernButton { Text = "📄 Tenant Template", StyleType = ButtonStyleType.Secondary, Location = new Point(10, y), Size = new Size(160, 34) };
            btnDownloadTenTmpl.Click += (s, e) => DownloadTemplate("Tenants_Template.csv", ImportExportService.GenerateTenantCsvTemplate());
            mainCard.Controls.Add(btnDownloadTenTmpl);

            var btnDownloadPropTmpl = new ModernButton { Text = "🏢 Property Template", StyleType = ButtonStyleType.Secondary, Location = new Point(180, y), Size = new Size(160, 34) };
            btnDownloadPropTmpl.Click += (s, e) => DownloadTemplate("Properties_Template.csv", ImportExportService.GeneratePropertyCsvTemplate());
            mainCard.Controls.Add(btnDownloadPropTmpl);
            y += 50;

            // SECTION 2: Import Tenants
            var lblSec2 = new Label { Text = "2. Bulk Import Customers / Tenants", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.Primary, Location = new Point(10, y), AutoSize = true };
            mainCard.Controls.Add(lblSec2);
            y += 28;

            txtTenantCsvPath = new TextBox { Location = new Point(10, y), Size = new Size(400, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "Select Tenants CSV file..." };
            mainCard.Controls.Add(txtTenantCsvPath);

            var btnBrowseTen = new ModernButton { Text = "Browse...", StyleType = ButtonStyleType.Secondary, Location = new Point(418, y - 2), Size = new Size(90, 30) };
            btnBrowseTen.Click += (s, e) => BrowseCsv(txtTenantCsvPath);
            mainCard.Controls.Add(btnBrowseTen);

            var btnImportTen = new ModernButton { Text = "📥 Import Tenants", StyleType = ButtonStyleType.Primary, Location = new Point(516, y - 4), Size = new Size(150, 34) };
            btnImportTen.Click += BtnImportTen_Click;
            mainCard.Controls.Add(btnImportTen);
            y += 50;

            // SECTION 3: Import Properties & Units
            var lblSec3 = new Label { Text = "3. Bulk Import Properties & Units", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.Primary, Location = new Point(10, y), AutoSize = true };
            mainCard.Controls.Add(lblSec3);
            y += 28;

            txtPropCsvPath = new TextBox { Location = new Point(10, y), Size = new Size(400, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "Select Properties CSV file..." };
            mainCard.Controls.Add(txtPropCsvPath);

            var btnBrowseProp = new ModernButton { Text = "Browse...", StyleType = ButtonStyleType.Secondary, Location = new Point(418, y - 2), Size = new Size(90, 30) };
            btnBrowseProp.Click += (s, e) => BrowseCsv(txtPropCsvPath);
            mainCard.Controls.Add(btnBrowseProp);

            var btnImportProp = new ModernButton { Text = "📥 Import Properties", StyleType = ButtonStyleType.Primary, Location = new Point(516, y - 4), Size = new Size(160, 34) };
            btnImportProp.Click += BtnImportProp_Click;
            mainCard.Controls.Add(btnImportProp);
            y += 50;

            // SECTION 4: Log Output Console
            var lblSec4 = new Label { Text = "Import Activity Log & Validation Results:", Font = ThemeColors.LabelBoldFont, ForeColor = ThemeColors.TextPrimary, Location = new Point(10, y), AutoSize = true };
            mainCard.Controls.Add(lblSec4);
            y += 26;

            txtLog = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(900, 220),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = ThemeColors.CanvasBg,
                Font = new Font("Consolas", 9f)
            };
            mainCard.Controls.Add(txtLog);

            Controls.Add(mainCard);

            mainCard.SendToBack();
            pnlHeader.BringToFront();
        }

        private void DownloadTemplate(string defaultName, string content)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = defaultName
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, content);
                ModernMessageBox.ShowInfo($"Template saved successfully to:\n{sfd.FileName}", "Template Saved", this);
            }
        }

        private void BrowseCsv(TextBox target)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select CSV File"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                target.Text = ofd.FileName;
            }
        }

        private void BtnImportTen_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenantCsvPath.Text) || !File.Exists(txtTenantCsvPath.Text))
            {
                ModernMessageBox.ShowWarning("Please select a valid CSV file.", "Validation", this);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Starting Tenant CSV Import from: {txtTenantCsvPath.Text}\r\n");

            var res = ImportExportService.ImportTenantsFromCsv(txtTenantCsvPath.Text);
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {res.Message}\r\n");

            if (res.Errors.Count > 0)
            {
                txtLog.AppendText("--- Validation Notices ---\r\n");
                foreach (var err in res.Errors)
                {
                    txtLog.AppendText($"• {err}\r\n");
                }
            }

            if (res.Success)
            {
                ModernMessageBox.ShowInfo(res.Message, "Tenant Import Complete", this);
            }
            else
            {
                ModernMessageBox.ShowError(res.Message, "Import Error", this);
            }
        }

        private void BtnImportProp_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPropCsvPath.Text) || !File.Exists(txtPropCsvPath.Text))
            {
                ModernMessageBox.ShowWarning("Please select a valid CSV file.", "Validation", this);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] Starting Property & Unit CSV Import from: {txtPropCsvPath.Text}\r\n");

            var res = ImportExportService.ImportPropertiesFromCsv(txtPropCsvPath.Text);
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {res.Message}\r\n");

            if (res.Errors.Count > 0)
            {
                txtLog.AppendText("--- Validation Notices ---\r\n");
                foreach (var err in res.Errors)
                {
                    txtLog.AppendText($"• {err}\r\n");
                }
            }

            if (res.Success)
            {
                ModernMessageBox.ShowInfo(res.Message, "Property Import Complete", this);
            }
            else
            {
                ModernMessageBox.ShowError(res.Message, "Import Error", this);
            }
        }
    }
}
