using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class SupplierView : UserControl
    {
        public SupplierView()
        {
            InitializeComponent();
            LoadItems();
        }

        private void LoadItems(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(s => s.Name.Contains(search));
            dgItems.ItemsSource = query
                .OrderBy(s => s.SupplierID)
                .ToList();
            dgItems.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadItems(txtSearch.Text.Trim());

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadItems();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string address = txtAddress.Text.Trim();
            string phone = txtPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nhập tên nhà cung cấp.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            using var ctx = new FamiContext();
            if (ctx.Suppliers.Any(s => s.Name == name))
            {
                MessageBox.Show("Nhà cung cấp đã tồn tại.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ctx.Suppliers.Add(new Supplier { Name = name, Address = address, Phone = phone });
            ctx.SaveChanges();
            ClearForm();
