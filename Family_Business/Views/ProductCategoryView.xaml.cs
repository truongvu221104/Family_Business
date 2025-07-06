using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class ProductCategoryView : UserControl
    {
        public ProductCategoryView()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.ProductCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.CategoryName.Contains(search));

            dgCategories.ItemsSource = query
                .OrderBy(c => c.CategoryID)
                .ToList();

            dgCategories.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadCategories(txtSearchCat.Text.Trim());

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearchCat.Clear();
            LoadCategories();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nhập tên loại sản phẩm trước khi thêm.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            if (ctx.ProductCategories.Any(c => c.CategoryName == name))
            {
                MessageBox.Show("Tên loại sản phẩm đã tồn tại.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cat = new ProductCategory { CategoryName = name };
            ctx.ProductCategories.Add(cat);
            ctx.SaveChanges();

            ClearForm();
            LoadCategories();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgCategories.SelectedItem is not ProductCategory selected)
            {
                MessageBox.Show("Chọn loại cần sửa.",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string newName = txtCategoryName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Nhập tên mới để sửa.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            var cat = ctx.ProductCategories.Find(selected.CategoryID);
            if (cat == null) return;

            if (ctx.ProductCategories
                   .Any(c => c.CategoryName == newName && c.CategoryID != cat.CategoryID))
            {
                MessageBox.Show("Tên loại đã tồn tại.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            cat.CategoryName = newName;
            ctx.SaveChanges();

            ClearForm();
            LoadCategories();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgCategories.SelectedItem is not ProductCategory selected) return;

            var result = MessageBox.Show(
                $"Bạn chắc chắn xóa loại: {selected.CategoryName}?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            using var ctx = new FamiContext();
            var cat = ctx.ProductCategories.Find(selected.CategoryID);
            if (cat != null)
            {
                ctx.ProductCategories.Remove(cat);
                ctx.SaveChanges();
            }

            ClearForm();
            LoadCategories();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgCategories.SelectedItem = null;
        }

        private void dgCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCategories.SelectedItem is ProductCategory selected)
                txtCategoryName.Text = selected.CategoryName;
            else
                ClearForm();
        }

        private void ClearForm()
        {
            txtCategoryName.Clear();
        }
    }
}
