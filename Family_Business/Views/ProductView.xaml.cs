using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            LoadCategories();
            LoadProducts();
        }

        private void LoadUnits()
        {
            using var ctx = new FamiContext();
            cbxUnit.ItemsSource = ctx.Units
                                     .OrderBy(u => u.UnitName)
                                     .ToList();
            cbxUnit.SelectedIndex = 0;
        }

        private void LoadCategories()
        {
            using var ctx = new FamiContext();
            cbxCategory.ItemsSource = ctx.ProductCategories
                                         .OrderBy(c => c.CategoryName)
                                         .ToList();
            cbxCategory.SelectedIndex = 0;
        }

        private class ProductDisplay
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string BaseUnitName { get; set; } = string.Empty;
            public int CategoryID { get; set; }
            public int BaseUnitId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal CostPerUnit { get; set; }
            public decimal RetailPrice { get; set; }
            public decimal WholesalePrice { get; set; }
            public string Note { get; set; } = string.Empty;
        }

        private void LoadProducts(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Products
                           .Include(p => p.BaseUnit)
                           .Include(p => p.Category)
                           .Include(p => p.ProductUnits)
                           .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            var list = query
                .OrderBy(p => p.ProductId)
                .ToList()
                .Select(p => new ProductDisplay
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    BaseUnitName = p.BaseUnit.UnitName,
                    CategoryName = p.Category.CategoryName,
                    BaseUnitId = p.BaseUnitId,
                    CategoryID = p.CategoryID,
                    CostPerUnit = p.ProductUnits
                                        .FirstOrDefault(u => u.UnitId == p.BaseUnitId)?
                                        .CostPerUnit ?? 0m,
                    RetailPrice = (p.ProductUnits
                                       .FirstOrDefault(u => u.UnitId == p.BaseUnitId)?
                                       .CostPerUnit ?? 0m) * (1 + p.RetailMarkupPercent / 100m),
                    WholesalePrice = (p.ProductUnits
                                       .FirstOrDefault(u => u.UnitId == p.BaseUnitId)?
                                       .CostPerUnit ?? 0m) * (1 + p.WholesaleMarkupPercent / 100m),
                    Note = p.Note ?? string.Empty
                })
                .ToList();

            dgProducts.ItemsSource = list;
            dgProducts.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadProducts(txtSearch.Text.Trim());

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadProducts();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtName.Text)
                || cbxUnit.SelectedItem is not Unit unit
                || cbxCategory.SelectedItem is not ProductCategory cat
                || !decimal.TryParse(txtCostPerUnit.Text, out var cost)
                || !decimal.TryParse(txtRetailMarkup.Text, out var rm)
                || !decimal.TryParse(txtWholesaleMarkup.Text, out var wm))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ và đúng định dạng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            // Tạo product
            var prod = new Product
            {
                Name = txtName.Text.Trim(),
                BaseUnitId = unit.UnitId,
                CategoryID = cat.CategoryID,
                RetailMarkupPercent = rm,
                WholesaleMarkupPercent = wm,
                Note = txtNote.Text.Trim()
            };
            ctx.Products.Add(prod);
            ctx.SaveChanges();

            // Tạo đơn vị giá nhập
            ctx.ProductUnits.Add(new ProductUnit
            {
                ProductId = prod.ProductId,
                UnitId = unit.UnitId,
                FactorToBase = 1m,
                CostPerUnit = cost
            });
            ctx.SaveChanges();

            ClearForm();
            LoadProducts();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not ProductDisplay row)
            {
                MessageBox.Show("Chọn sản phẩm để sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text)
                || cbxUnit.SelectedItem is not Unit unit
                || cbxCategory.SelectedItem is not ProductCategory cat
                || !decimal.TryParse(txtCostPerUnit.Text, out var cost)
                || !decimal.TryParse(txtRetailMarkup.Text, out var rm)
                || !decimal.TryParse(txtWholesaleMarkup.Text, out var wm))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ và đúng định dạng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            var prod = ctx.Products
                          .Include(p => p.ProductUnits)
                          .FirstOrDefault(p => p.ProductId == row.ProductId);
            if (prod == null) return;

            // Cập nhật product
            prod.Name = txtName.Text.Trim();
            prod.BaseUnitId = unit.UnitId;
            prod.CategoryID = cat.CategoryID;
            prod.RetailMarkupPercent = rm;
            prod.WholesaleMarkupPercent = wm;
            prod.Note = txtNote.Text.Trim();
            ctx.SaveChanges();

            // Cập nhật giá nhập
            var pu = prod.ProductUnits.FirstOrDefault(u => u.UnitId == prod.BaseUnitId);
            if (pu != null) pu.CostPerUnit = cost;
            ctx.SaveChanges();

            ClearForm();
            LoadProducts();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem is not ProductDisplay row) return;

            if (MessageBox.Show(
                $"Bạn chắc chắn xóa sản phẩm: {row.Name}?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var ctx = new FamiContext();
            ctx.ProductUnits.RemoveRange(
                ctx.ProductUnits.Where(u => u.ProductId == row.ProductId));
            var prod = ctx.Products.Find(row.ProductId);
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
            if (dgProducts.SelectedItem is ProductDisplay row)
            {
                txtName.Text = row.Name;
                txtCostPerUnit.Text = row.CostPerUnit.ToString();
                txtRetailMarkup.Text = ((row.RetailPrice / row.CostPerUnit - 1) * 100)
                        .ToString("F2");
                txtWholesaleMarkup.Text = ((row.WholesalePrice / row.CostPerUnit - 1) * 100)
                                          .ToString("F2");
                txtNote.Text = row.Note;

                cbxUnit.SelectedValue = row.BaseUnitId;
                cbxCategory.SelectedValue = row.CategoryID;
            }
            else
            {
                ClearForm();
            }
        }
        private void BtnManageUnits_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "Quản lý Đơn vị",
                Content = new UnitView(),
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
            LoadUnits();    // reload lại danh sách ĐVT
        }

        private void BtnManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var win = new Window
            {
                Title = "Quản lý Loại sản phẩm",
                Content = new ProductCategoryView(),
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            win.ShowDialog();
            LoadCategories();  // reload lại danh sách Loại SP
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtSearch.Clear();
            txtCostPerUnit.Clear();
            txtRetailMarkup.Clear();
            txtWholesaleMarkup.Clear();
            txtNote.Clear();
            cbxUnit.SelectedIndex = 0;
            cbxCategory.SelectedIndex = 0;
        }
    }
}