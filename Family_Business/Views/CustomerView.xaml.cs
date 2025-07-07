using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class CustomerView : UserControl
    {
        public CustomerView()
        {
            InitializeComponent();
            LoadItems();
        }

        private void LoadItems(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));
            dgItems.ItemsSource = query
                .OrderBy(c => c.CustomerID)
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
                MessageBox.Show("Nhập tên khách hàng.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            using var ctx = new FamiContext();
            if (ctx.Customers.Any(c => c.Name == name))
            {
                MessageBox.Show("Khách hàng đã tồn tại.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ctx.Customers.Add(new Customer { Name = name, Address = address, Phone = phone });
            ctx.SaveChanges();
            ClearForm();
            LoadItems();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgItems.SelectedItem is not Customer selected)
            {
                MessageBox.Show("Chọn khách hàng để sửa.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string name = txtName.Text.Trim();
            string address = txtAddress.Text.Trim();
            string phone = txtPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nhập tên mới.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            using var ctx = new FamiContext();
            var cust = ctx.Customers.Find(selected.CustomerID);
            if (cust != null)
            {
                if (ctx.Customers.Any(c => c.Name == name && c.CustomerID != cust.CustomerID))
                {
                    MessageBox.Show("Tên đã tồn tại.", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                cust.Name = name;
                cust.Address = address;
                cust.Phone = phone;
                ctx.SaveChanges();
            }
            ClearForm();
            LoadItems();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgItems.SelectedItem is not Customer selected) return;
            if (MessageBox.Show($"Bạn chắc chắn xóa: {selected.Name}?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            using var ctx = new FamiContext();
            var cust = ctx.Customers.Find(selected.CustomerID);
            if (cust != null)
            {
                ctx.Customers.Remove(cust);
                ctx.SaveChanges();
            }
            ClearForm();
            LoadItems();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void dgItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgItems.SelectedItem is Customer selected)
            {
                txtName.Text = selected.Name;
                txtAddress.Text = selected.Address;
                txtPhone.Text = selected.Phone;
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtAddress.Clear();
            txtPhone.Clear();
            dgItems.SelectedItem = null;
        }
    }
}