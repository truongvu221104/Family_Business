using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public partial class ProductView : UserControl
    {
        public ProductView()
        {
            InitializeComponent();
            LoadUnits();
            LoadProducts();
        }

        private void LoadUnits()
        {
            using var ctx = new FamiContext();
            cbxUnit.ItemsSource = ctx.Units.ToList();
            cbxUnit.SelectedIndex = 0;
        }

        private void LoadProducts(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Products.Include("BaseUnit").AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));
            dgProducts.ItemsSource = query.OrderBy(p => p.ProductId).ToList();
            dgProducts.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts(txtSearch.Text.Trim());
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadProducts();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            var baseUnit = cbxUnit.SelectedItem as Unit;
            if (string.IsNullOrWhiteSpace(name) || baseUnit == null)
            {
                MessageBox.Show("Nhập tên sản phẩm và chọn đơn vị.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtMarkup.Text, out decimal markup))
            {
                MessageBox.Show("Lợi nhuận (%) phải là số!", "Sai định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string note = txtNote.Text.Trim();

            using var ctx = new FamiContext();
            var product = new Product
            {
                Name = name,
                BaseUnitId = baseUnit.UnitId,
                MarkupPercent = markup,
                Note = note
            };
            ctx.Products.Add(product);
            ctx.SaveChanges();

            ClearForm();
            LoadProducts();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not Product selected)
            {
                MessageBox.Show("Chọn sản phẩm cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string name = txtName.Text.Trim();
            var baseUnit = cbxUnit.SelectedItem as Unit;
            if (string.IsNullOrWhiteSpace(name) || baseUnit == null)
            {
                MessageBox.Show("Nhập tên sản phẩm và chọn đơn vị.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtMarkup.Text, out decimal markup))
            {
                MessageBox.Show("Lợi nhuận (%) phải là số!", "Sai định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string note = txtNote.Text.Trim();

            using var ctx = new FamiContext();
            var prod = ctx.Products.Find(selected.ProductId);
            if (prod == null) return;
            prod.Name = name;
            prod.BaseUnitId = baseUnit.UnitId;
            prod.MarkupPercent = markup;
            prod.Note = note;
            ctx.SaveChanges();

            ClearForm();
            LoadProducts();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not Product selected) return;
            if (MessageBox.Show($"Bạn chắc chắn xóa sản phẩm: {selected.Name}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var ctx = new FamiContext();
            var prod = ctx.Products.Find(selected.ProductId);
            if (prod != null)
            {
                ctx.Products.Remove(prod);
                ctx.SaveChanges();
            }
            ClearForm();
            LoadProducts();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgProducts.SelectedItem = null;
        }

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProducts.SelectedItem is Product selected)
            {
                txtName.Text = selected.Name;
                cbxUnit.SelectedItem = cbxUnit.Items.OfType<Unit>().FirstOrDefault(u => u.UnitId == selected.BaseUnitId);
                txtMarkup.Text = selected.MarkupPercent.ToString();
                txtNote.Text = selected.Note;
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtName.Clear();
            cbxUnit.SelectedIndex = 0;
            txtMarkup.Clear();
            txtNote.Clear();
        }
    }
}
