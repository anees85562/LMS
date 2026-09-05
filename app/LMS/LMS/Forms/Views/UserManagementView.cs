using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LMS.Data;
using LMS.Forms.Dialogs;
using LMS.Models;
using LMS.Services;
using LMS.UI;
using LMS.UI.Controls;

namespace LMS.Forms.Views
{
    public class UserManagementView : UserControl
    {
        private ModernButton btnAddUser = null!;
        private ModernButton btnEditUser = null!;
        private ModernButton btnToggleActive = null!;
        private ModernDataGridView dgvUsers = null!;

        public UserManagementView()
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
                "🔐 User Accounts & Role-Based Security Control",
                "Manage system operator accounts, assign roles (Administrator, Operator, Viewer), and reset credentials."
            );
            Controls.Add(pnlHeader);

            // 2. Action Toolbar
            var filterBar = UIHelper.CreateFilterBar(54);
            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            btnAddUser = new ModernButton { Text = "+ Create New User", StyleType = ButtonStyleType.Primary, Size = new Size(160, 34), Margin = new Padding(0, 0, 8, 0) };
            btnAddUser.Click += BtnAddUser_Click;
            actionFlow.Controls.Add(btnAddUser);

            btnEditUser = new ModernButton { Text = "Edit Selected", StyleType = ButtonStyleType.Secondary, Size = new Size(130, 34), Margin = new Padding(0, 0, 8, 0) };
            btnEditUser.Click += BtnEditUser_Click;
            actionFlow.Controls.Add(btnEditUser);

            btnToggleActive = new ModernButton { Text = "Toggle Active Status", StyleType = ButtonStyleType.Secondary, Size = new Size(160, 34), Margin = new Padding(0, 0, 0, 0) };
            btnToggleActive.Click += BtnToggleActive_Click;
            actionFlow.Controls.Add(btnToggleActive);

            filterBar.Controls.Add(actionFlow);
            Controls.Add(filterBar);

            // 3. Grid Container
            var pnlGridCard = UIHelper.CreateCardPanel(new Padding(1));
            pnlGridCard.Dock = DockStyle.Fill;
            pnlGridCard.Margin = new Padding(0, 10, 0, 10);

            dgvUsers = new ModernDataGridView { Dock = DockStyle.Fill };
            dgvUsers.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnEditUser_Click(this, EventArgs.Empty); };
            pnlGridCard.Controls.Add(dgvUsers);

            Controls.Add(pnlGridCard);

            // Z-Order
            pnlGridCard.SendToBack();
            pnlHeader.BringToFront();

            Load += (s, e) => LoadUsers();
        }

        public void LoadUsers()
        {
            using var db = new AppDbContext();
            var users = db.Users.OrderBy(u => u.Role).ThenBy(u => u.Username).ToList();

            var list = users.Select(u => new
            {
                Username = u.Username,
                FullName = u.FullName,
                Role = u.Role.ToString(),
                Status = u.IsActive ? "Active" : "Disabled",
                LastLogin = u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm") : "Never",
                CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy"),
                Id = u.Id
            }).ToList();

            dgvUsers.DataSource = list;
            if (dgvUsers.Columns.Contains("Id")) dgvUsers.Columns["Id"].Visible = false;
        }

        private void BtnAddUser_Click(object? sender, EventArgs e)
        {
            using var dlg = new UserEditForm();
            if (dlg.ShowDialog() == DialogResult.OK) LoadUsers();
        }

        private void BtnEditUser_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow == null || !dgvUsers.Columns.Contains("Id")) return;
            int userId = Convert.ToInt32(dgvUsers.CurrentRow.Cells["Id"].Value);

            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user != null)
            {
                using var dlg = new UserEditForm(user);
                if (dlg.ShowDialog() == DialogResult.OK) LoadUsers();
            }
        }

        private void BtnToggleActive_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.CurrentRow == null || !dgvUsers.Columns.Contains("Id")) return;
            int userId = Convert.ToInt32(dgvUsers.CurrentRow.Cells["Id"].Value);

            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user != null)
            {
                var res = AuthService.UpdateUser(user.Id, user.FullName, user.Role, !user.IsActive);
                if (res.Success)
                {
                    LoadUsers();
                }
                else
                {
                    ModernMessageBox.ShowWarning(res.Message, "Warning", this);
                }
            }
        }
    }
}
