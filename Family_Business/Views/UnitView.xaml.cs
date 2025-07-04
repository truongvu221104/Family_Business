using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class UnitView : UserControl
    {
        public UnitView()
        {
            InitializeComponent();
            LoadUnits();
        }

        private void LoadUnits(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Units.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.UnitName.Contains(search));
            dgUnits.ItemsSource = query.OrderBy(u => u.UnitId).ToList();
            dgUnits.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadUnits(txtSearch.Text.Trim());
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadUnits();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtUnitName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nhập tên đơn vị trước khi thêm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            if (ctx.Units.Any(u => u.UnitName == name))
            {
                MessageBox.Show("Tên đơn vị đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var u = new Unit { UnitName = name };
            ctx.Units.Add(u);
            ctx.SaveChanges();

            ClearForm();
            LoadUnits();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgUnits.SelectedItem is not Unit selected)
            {
                MessageBox.Show("Chọn đơn vị cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string newName = txtUnitName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Nhập tên mới để sửa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            var unit = ctx.Units.Find(selected.UnitId);
            if (unit == null) return;

            if (ctx.Units.Any(u => u.UnitName == newName && u.UnitId != unit.UnitId))
            {
                MessageBox.Show("Tên đơn vị đã tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            unit.UnitName = newName;
            ctx.SaveChanges();

            ClearForm();
            LoadUnits();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgUnits.SelectedItem is not Unit selected) return;

            if (MessageBox.Show($"Bạn chắc chắn xóa đơn vị: {selected.UnitName}?",
                                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var ctx = new FamiContext();
            var unit = ctx.Units.Find(selected.UnitId);
            if (unit != null)
            {
                ctx.Units.Remove(unit);
                ctx.SaveChanges();
            }
            ClearForm();
            LoadUnits();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgUnits.SelectedItem = null;
        }

        private void dgUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUnits.SelectedItem is Unit selected)
                txtUnitName.Text = selected.UnitName;
            else
                ClearForm();
        }

        private void ClearForm()
        {
            txtUnitName.Clear();
        }
    }
}
