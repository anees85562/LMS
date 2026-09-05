using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class BackupRestoreView : UserControl
    {
        private Label lblCurrentDir = null!;
        private ModernButton btnChangeDir = null!;
        private ModernButton btnBackupNow = null!;
        private ModernButton btnRestore = null!;
        private ModernDataGridView dgvHistory = null!;

        public BackupRestoreView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Dock = DockStyle.Fill;
            BackColor = ThemeColors.CanvasBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(20, 16, 20, 20);

            // 1. Header
            var pnlHeader = UIHelper.CreatePageHeader(
                "💾 Database Backup, Safe Restore & Disaster Recovery",
                "100% offline snapshot backups, automated retention cleanup, and safe cryptographic database restore protocols."
            );
            Controls.Add(pnlHeader);

            // 2. Action Card
            var pnlActionCard = UIHelper.CreateCardPanel(new Padding(16));
            pnlActionCard.Dock = DockStyle.Top;
            pnlActionCard.Height = 110;

            lblCurrentDir = new Label
            {
                Text = $"Backup Directory: {BackupService.GetDefaultBackupDirectory()}",
                Dock = DockStyle.Top,
                Height = 22,
                Font = ThemeColors.LabelBoldFont,
                ForeColor = ThemeColors.TextPrimary
            };
            pnlActionCard.Controls.Add(lblCurrentDir);

            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            btnChangeDir = new ModernButton
            {
                Text = "Change Location...",
                StyleType = ButtonStyleType.Secondary,
                Size = new Size(150, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnChangeDir.Click += BtnChangeDir_Click;
            actionFlow.Controls.Add(btnChangeDir);

            btnBackupNow = new ModernButton
            {
                Text = "💾 Backup Database Now",
                StyleType = ButtonStyleType.Primary,
                Size = new Size(200, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnBackupNow.Click += BtnBackupNow_Click;
            actionFlow.Controls.Add(btnBackupNow);

            btnRestore = new ModernButton
            {
                Text = "🔄 Restore from Backup...",
                StyleType = ButtonStyleType.Danger,
                Size = new Size(200, 36),
                Margin = new Padding(0, 0, 0, 0)
            };
            btnRestore.Click += BtnRestore_Click;
            actionFlow.Controls.Add(btnRestore);

            pnlActionCard.Controls.Add(actionFlow);
            Controls.Add(pnlActionCard);

            // 3. History Section Header
            var pnlHistHeader = UIHelper.CreateSectionHeader("📋 Backup History & Snapshot Archive");
            pnlHistHeader.Dock = DockStyle.Top;
            pnlHistHeader.Height = 35;
            pnlHistHeader.Margin = new Padding(0, 8, 0, 0);
            Controls.Add(pnlHistHeader);

            // 4. Grid Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 6, 0, 0);

            dgvHistory = new ModernDataGridView { Dock = DockStyle.Fill };
            pnlGridCard.Controls.Add(dgvHistory);
            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += (s, e) => LoadHistory();
        }

        public void LoadHistory()
        {
            lblCurrentDir.Text = $"Backup Directory: {BackupService.GetDefaultBackupDirectory()}";

            var history = BackupService.GetBackupHistory();
            var list = history.Select(h => new
            {
                Date = h.BackupDate.ToString("dd/MM/yyyy HH:mm:ss"),
                FileName = Path.GetFileName(h.FilePath),
                Size = $"{h.FileSizeBytes / 1024.0:N1} KB",
                Type = h.BackupType.ToString(),
                Status = h.IsVerified ? "Verified" : "Unverified",
                Notes = h.Notes ?? "-",
                FilePath = h.FilePath
            }).ToList();

            dgvHistory.DataSource = list;
            if (dgvHistory.Columns.Contains("FilePath"))
            {
                dgvHistory.Columns["FilePath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void BtnChangeDir_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Select Default Backup Directory",
                SelectedPath = BackupService.GetDefaultBackupDirectory()
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                SettingService.Set("Backup.DefaultDirectory", fbd.SelectedPath, "Backup", "Default Backup Directory");
                LoadHistory();
            }
        }

        private void BtnBackupNow_Click(object? sender, EventArgs e)
        {
            var res = BackupService.CreateBackup(null, BackupType.Manual);
            if (res.Success)
            {
                ModernMessageBox.ShowInfo($"Backup created successfully!\nFile: {res.BackupFilePath}", "Backup Completed", this);
                LoadHistory();
            }
            else
            {
                ModernMessageBox.ShowError(res.Message, "Backup Failed", this);
            }
        }

        private void BtnRestore_Click(object? sender, EventArgs e)
        {
            if (!AuthService.HasPermission("RestoreDatabase"))
            {
                ModernMessageBox.ShowWarning("Only administrators are authorized to restore the database.", "Permission Denied", this);
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "Select Landlord Database Backup File (.db)",
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                InitialDirectory = BackupService.GetDefaultBackupDirectory()
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string backupFile = ofd.FileName;
                var inspection = BackupService.InspectBackupFile(backupFile);

                if (!inspection.IsValid)
                {
                    ModernMessageBox.ShowError($"Selected file is invalid or corrupted.\n{inspection.Info}", "Invalid Backup File", this);
                    return;
                }

                string msg = $"You are about to restore database from:\n{Path.GetFileName(backupFile)}\n\nBackup Database Contents:\n• Properties: {inspection.PropertyCount}\n• Tenants: {inspection.TenantCount}\n• Financial Transactions: {inspection.TransactionCount}\n\n⚠️ WARNING: Your current database will be replaced with this backup.\nA safety snapshot of your current database will be created automatically before restoring.\n\nAre you sure you want to proceed with database restore?";

                if (ModernMessageBox.ShowConfirm(msg, "Confirm Database Restore", this))
                {
                    var res = BackupService.RestoreBackup(backupFile);
                    if (res.Success)
                    {
                        ModernMessageBox.ShowInfo(res.Message, "Restore Succeeded", this);
                        LoadHistory();
                    }
                    else
                    {
                        ModernMessageBox.ShowError(res.Message, "Restore Failed", this);
                    }
                }
            }
        }
    }
}
