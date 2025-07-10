using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

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
            {
                // 1) Tìm theo số điện thoại (Contains bình thường đủ tốc độ)
                // 2) Tìm theo tên accent-insensitive, case-insensitive bằng COLLATE
                var pattern = $"%{search}%";
                query = query.Where(c =>
                    EF.Functions.Like(c.PhoneNumber, pattern)
                    || EF.Functions.Like(
                           EF.Functions.Collate(c.Name, "Vietnamese_CI_AI"),
                           pattern
                       )
                );
            }

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
            string addr = txtAddress.Text.Trim();
            string phone = txtPhone.Text.Trim();

            // 1. Bắt buộc nhập tên
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Tên khách hàng không được để trống.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Bắt buộc nhập phone
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Số điện thoại không được để trống.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 3. Kiểm tra format phone
            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại phải bắt đầu bằng '0' và gồm đúng 10 chữ số.",
                                "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            // 4. Kiểm tra trùng phone trong Customer
            if (ctx.Customers.Any(c => c.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã tồn tại trong danh sách Khách hàng.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 5. Kiểm tra trùng phone qua Supplier
            if (ctx.Suppliers.Any(s => s.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã có trong danh sách Nhà cung cấp.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cust = new Customer
            {
                Name = name,
                Address = string.IsNullOrEmpty(addr) ? null : addr,
                PhoneNumber = phone
            };

            ctx.Customers.Add(cust);
            ctx.SaveChanges();

            ClearForm();
            LoadItems();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgItems.SelectedItem is not Customer selected)
            {
                MessageBox.Show("Chọn khách hàng cần sửa.",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = txtName.Text.Trim();
            string addr = txtAddress.Text.Trim();
            string phone = txtPhone.Text.Trim();

            // 1. Bắt buộc nhập tên
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Tên khách hàng không được để trống.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Bắt buộc nhập phone
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Số điện thoại không được để trống.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 3. Kiểm tra format phone
            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại phải bắt đầu bằng '0' và gồm đúng 10 chữ số.",
                                "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            var cust = ctx.Customers.Find(selected.CustomerID);
            if (cust == null) return;

            // 4. Kiểm tra trùng phone trong Customer (ngoại trừ chính nó)
            if (ctx.Customers.Any(c => c.PhoneNumber == phone && c.CustomerID != cust.CustomerID))
            {
                MessageBox.Show("Số điện thoại này đã tồn tại trong danh sách Khách hàng.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 5. Kiểm tra trùng phone qua Supplier
            if (ctx.Suppliers.Any(s => s.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã có trong danh sách Nhà cung cấp.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 6. Cập nhật
            cust.Name = name;
            cust.Address = string.IsNullOrEmpty(addr) ? null : addr;
            cust.PhoneNumber = phone;

            ctx.SaveChanges();

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
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // search theo tên hoặc số điện thoại
            LoadItems(txtSearch.Text.Trim());
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
                txtPhone.Text = selected.PhoneNumber;
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