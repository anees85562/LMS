using System;
using System.Collections.Generic;
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
    public class PropertyManagementView : UserControl
    {
        private TextBox txtSearchProp = null!;
        private ComboBox cmbStatusFilter = null!;
        private ModernButton btnAddProp = null!;
        private ModernButton btnEditProp = null!;
        private ModernButton btnArchiveProp = null!;
        private ModernDataGridView dgvProperties = null!;

        private Label lblSelectedPropTitle = null!;
        private ModernButton btnAddUnit = null!;
        private ModernButton btnEditUnit = null!;
        private ModernButton btnDeleteUnit = null!;
        private ModernDataGridView dgvUnits = null!;

        private List<Property> _allProps = new();
        private Property? _selectedProp;

        public PropertyManagementView()
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
                "🏢 Property & Unit Portfolio Management",
                "Manage your real estate assets, buildings, plazas, commercial markets, and individual rental units or shops."
            );
            Controls.Add(pnlHeader);

            // 2. Split Container for Properties (Left) and Units (Right)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600,
                BackColor = ThemeColors.Border,
                Margin = new Padding(0, 8, 0, 0)
            };

            // LEFT PANEL: Properties
            var pnlLeft = UIHelper.CreateCardPanel(new Padding(12));
            pnlLeft.Dock = DockStyle.Fill;

            var pnlLeftTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            var lblLeftHeader = new Label { Text = "Properties & Buildings", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.TextPrimary, Location = new Point(0, 4), AutoSize = true };
            pnlLeftTop.Controls.Add(lblLeftHeader);

            var propFilterFlow = new FlowLayoutPanel { Location = new Point(0, 36), Size = new Size(570, 38), WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };

            txtSearchProp = new TextBox { Size = new Size(160, 26), Font = new Font("Segoe UI", 9.5f), PlaceholderText = "Search property...", Margin = new Padding(0, 3, 6, 0) };
            txtSearchProp.TextChanged += (s, e) => FilterProperties();
            propFilterFlow.Controls.Add(txtSearchProp);

            cmbStatusFilter = new ComboBox { Size = new Size(110, 26), Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 3, 8, 0) };
            cmbStatusFilter.Items.AddRange(new object[] { "Active Only", "All Properties" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadProperties();
            propFilterFlow.Controls.Add(cmbStatusFilter);

            btnAddProp = new ModernButton { Text = "+ Add", StyleType = ButtonStyleType.Primary, Size = new Size(70, 32), Margin = new Padding(0, 0, 4, 0) };
            btnAddProp.Click += BtnAddProp_Click;
            propFilterFlow.Controls.Add(btnAddProp);

            btnEditProp = new ModernButton { Text = "Edit", StyleType = ButtonStyleType.Secondary, Size = new Size(65, 32), Margin = new Padding(0, 0, 4, 0) };
            btnEditProp.Click += BtnEditProp_Click;
            propFilterFlow.Controls.Add(btnEditProp);

            btnArchiveProp = new ModernButton { Text = "Archive", StyleType = ButtonStyleType.Danger, Size = new Size(75, 32), Margin = new Padding(0, 0, 0, 0) };
            btnArchiveProp.Click += BtnArchiveProp_Click;
            propFilterFlow.Controls.Add(btnArchiveProp);

            pnlLeftTop.Controls.Add(propFilterFlow);
            pnlLeft.Controls.Add(pnlLeftTop);

            dgvProperties = new ModernDataGridView { Dock = DockStyle.Fill };
            dgvProperties.SelectionChanged += DgvProperties_SelectionChanged;
            pnlLeft.Controls.Add(dgvProperties);
            dgvProperties.BringToFront();

            split.Panel1.Controls.Add(pnlLeft);

            // RIGHT PANEL: Units in Selected Property
            var pnlRight = UIHelper.CreateCardPanel(new Padding(12));
            pnlRight.Dock = DockStyle.Fill;

            var pnlRightTop = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent };
            lblSelectedPropTitle = new Label { Text = "Units in Selected Property", Font = ThemeColors.SectionFont, ForeColor = ThemeColors.TextPrimary, Location = new Point(0, 4), AutoSize = true };
            pnlRightTop.Controls.Add(lblSelectedPropTitle);

            var unitFilterFlow = new FlowLayoutPanel { Location = new Point(0, 36), Size = new Size(500, 38), WrapContents = false, AutoScroll = true, BackColor = Color.Transparent };

            btnAddUnit = new ModernButton { Text = "+ Add Unit", StyleType = ButtonStyleType.Primary, Size = new Size(95, 32), Margin = new Padding(0, 0, 6, 0) };
            btnAddUnit.Click += BtnAddUnit_Click;
            unitFilterFlow.Controls.Add(btnAddUnit);

            btnEditUnit = new ModernButton { Text = "Edit Unit", StyleType = ButtonStyleType.Secondary, Size = new Size(90, 32), Margin = new Padding(0, 0, 6, 0) };
            btnEditUnit.Click += BtnEditUnit_Click;
            unitFilterFlow.Controls.Add(btnEditUnit);

            btnDeleteUnit = new ModernButton { Text = "Delete Unit", StyleType = ButtonStyleType.Danger, Size = new Size(95, 32), Margin = new Padding(0, 0, 0, 0) };
            btnDeleteUnit.Click += BtnDeleteUnit_Click;
            unitFilterFlow.Controls.Add(btnDeleteUnit);

            pnlRightTop.Controls.Add(unitFilterFlow);
            pnlRight.Controls.Add(pnlRightTop);

            dgvUnits = new ModernDataGridView { Dock = DockStyle.Fill };
            pnlRight.Controls.Add(dgvUnits);
            dgvUnits.BringToFront();

            split.Panel2.Controls.Add(pnlRight);
            Controls.Add(split);

            split.SendToBack();
            pnlHeader.BringToFront();

            Load += (s, e) => LoadProperties();
        }

        public void LoadProperties()
        {
            bool includeInactive = cmbStatusFilter.SelectedIndex == 1;
            _allProps = PropertyService.GetAllProperties(includeInactive);
            FilterProperties();
        }

        private void FilterProperties()
        {
            string s = txtSearchProp.Text.Trim().ToLower();
            var list = _allProps.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(s))
            {
                list = list.Where(p => p.Name.ToLower().Contains(s) || p.PropertyCode.ToLower().Contains(s) || (p.City != null && p.City.ToLower().Contains(s)));
            }

            var table = list.Select(p => new
            {
                Code = p.PropertyCode,
                Name = p.Name,
                Type = p.PropertyType,
                City = p.City ?? "-",
                Units = p.Units.Count,
                Status = p.Status.ToString(),
                Id = p.Id
            }).ToList();

            dgvProperties.DataSource = table;
            if (dgvProperties.Columns.Contains("Id")) dgvProperties.Columns["Id"].Visible = false;
        }

        private void DgvProperties_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvProperties.CurrentRow != null && dgvProperties.CurrentRow.Cells["Id"].Value is int propId)
            {
                _selectedProp = _allProps.FirstOrDefault(p => p.Id == propId);
                LoadUnitsForSelectedProperty();
            }
            else
            {
                _selectedProp = null;
                dgvUnits.DataSource = null;
                lblSelectedPropTitle.Text = "Units (No Property Selected)";
            }
        }

        private void LoadUnitsForSelectedProperty()
        {
            if (_selectedProp == null) return;

            lblSelectedPropTitle.Text = $"Units in: {_selectedProp.Name} ({_selectedProp.PropertyCode})";
            var units = PropertyService.GetUnitsByPropertyId(_selectedProp.Id);

            var unitList = units.Select(u =>
            {
                var lease = u.RentAgreements.FirstOrDefault(a => a.Status == AgreementStatus.Active);
                return new
                {
                    Unit = u.UnitNumber,
                    Type = u.UnitType,
                    Floor = u.Floor ?? "-",
                    BaseRent = u.BaseRent.ToString("N0"),
                    Status = u.Status.ToString(),
                    Tenant = lease?.Tenant?.FullName ?? "-",
                    Id = u.Id
                };
            }).ToList();

            dgvUnits.DataSource = unitList;
            if (dgvUnits.Columns.Contains("Id")) dgvUnits.Columns["Id"].Visible = false;
        }

        private void BtnAddProp_Click(object? sender, EventArgs e)
        {
            using var dlg = new PropertyEditForm();
            if (dlg.ShowDialog() == DialogResult.OK) LoadProperties();
        }

        private void BtnEditProp_Click(object? sender, EventArgs e)
        {
            if (_selectedProp == null) return;
            using var dlg = new PropertyEditForm(_selectedProp);
            if (dlg.ShowDialog() == DialogResult.OK) LoadProperties();
        }

        private void BtnArchiveProp_Click(object? sender, EventArgs e)
        {
            if (_selectedProp == null) return;
            if (ModernMessageBox.ShowConfirm($"Are you sure you want to delete or archive property '{_selectedProp.Name}'?", "Confirm Delete/Archive", this))
            {
                var res = PropertyService.DeleteOrArchiveProperty(_selectedProp.Id);
                ModernMessageBox.ShowInfo(res.Message, "Result", this);
                LoadProperties();
            }
        }

        private void BtnAddUnit_Click(object? sender, EventArgs e)
        {
            if (_selectedProp == null)
            {
                ModernMessageBox.ShowInfo("Please select a property first.", "Selection Required", this);
                return;
            }

            using var dlg = new UnitEditForm(_selectedProp.Id);
            if (dlg.ShowDialog() == DialogResult.OK) LoadProperties();
        }

        private void BtnEditUnit_Click(object? sender, EventArgs e)
        {
            if (dgvUnits.CurrentRow == null || !dgvUnits.Columns.Contains("Id")) return;
            int unitId = Convert.ToInt32(dgvUnits.CurrentRow.Cells["Id"].Value);

            var unit = PropertyService.GetAllUnits().FirstOrDefault(u => u.Id == unitId);
            if (unit != null)
            {
                using var dlg = new UnitEditForm(unit.PropertyId, unit);
                if (dlg.ShowDialog() == DialogResult.OK) LoadProperties();
            }
        }

        private void BtnDeleteUnit_Click(object? sender, EventArgs e)
        {
            if (dgvUnits.CurrentRow == null || !dgvUnits.Columns.Contains("Id")) return;
            int unitId = Convert.ToInt32(dgvUnits.CurrentRow.Cells["Id"].Value);

            if (ModernMessageBox.ShowConfirm("Are you sure you want to delete this unit?", "Confirm", this))
            {
                var res = PropertyService.DeleteUnit(unitId);
                if (res.Success)
                {
                    ModernMessageBox.ShowInfo(res.Message, "Deleted", this);
                    LoadProperties();
                }
                else
                {
                    ModernMessageBox.ShowWarning(res.Message, "Cannot Delete", this);
                }
            }
        }
    }
}
