using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public partial class SupplierView : UserControl
    {
        public SupplierView()
        {
            InitializeComponent();
            LoadSuppliers();
        }

        private void LoadSuppliers(string search = "")
        {
            using var ctx = new FamiContext();
            var query = ctx.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search}%";

                query = query.Where(s =>
                    // tìm số điện thoại
                    EF.Functions.Like(s.PhoneNumber, pattern)
                    // hoặc tìm tên accent-insensitive
                    || EF.Functions.Like(
                           EF.Functions.Collate(s.Name, "Vietnamese_CI_AI"),
                           pattern
                       )
                );
            }

            dgSuppliers.ItemsSource = query
                .OrderBy(s => s.SupplierId)
                .ToList();

            dgSuppliers.SelectedItem = null;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadSuppliers(txtSearch.Text.Trim());
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
            => LoadSuppliers(txtSearch.Text.Trim());

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            LoadSuppliers();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string addr = txtAddress.Text.Trim();
            string phone = txtPhoneNumber.Text.Trim();

            // 1. Validate bắt buộc nhập phone
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Số điện thoại không được để trống.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 2. Validate định dạng phone: phải bắt đầu 0 và 10 chữ số
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại phải bắt đầu bằng '0' và gồm đúng 10 chữ số.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validate tên phải có
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Tên nhà cung cấp không được để trống.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            // 4. Check trùng phone trong bảng này
            if (ctx.Suppliers.Any(s => s.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã tồn tại.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 5. Check trùng phone qua Customer (nếu cần)
            if (ctx.Customers.Any(c => c.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã có trong danh sách Khách hàng.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sup = new Supplier
            {
                Name = name,
                Address = addr,
                PhoneNumber = phone
            };

            ctx.Suppliers.Add(sup);
            ctx.SaveChanges();

            ClearForm();
            LoadSuppliers();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgSuppliers.SelectedItem is not Supplier selected)
            {
                MessageBox.Show("Chọn nhà cung cấp cần sửa.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = txtName.Text.Trim();
            string addr = txtAddress.Text.Trim();
            string phone = txtPhoneNumber.Text.Trim();

            // 1. Validate bắt buộc nhập phone
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Số điện thoại không được để trống.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 2. Validate định dạng phone
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại phải bắt đầu bằng '0' và gồm đúng 10 chữ số.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validate tên
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Tên nhà cung cấp không được để trống.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            var sup = ctx.Suppliers.Find(selected.SupplierId);
            if (sup == null) return;

            // 4. Check trùng phone (ngoại trừ chính nó)
            if (ctx.Suppliers.Any(s => s.PhoneNumber == phone && s.SupplierId != sup.SupplierId))
            {
                MessageBox.Show("Số điện thoại này đã tồn tại.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 5. Check trùng phone qua Customer
            if (ctx.Customers.Any(c => c.PhoneNumber == phone))
            {
                MessageBox.Show("Số điện thoại này đã có trong danh sách Khách hàng.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 6. Cập nhật
            sup.Name = name;
            sup.Address = addr;
            sup.PhoneNumber = phone;

            ctx.SaveChanges();

            ClearForm();
            LoadSuppliers();
        }


        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgSuppliers.SelectedItem is not Supplier selected) return;

            if (MessageBox.Show(
                    $"Bạn chắc chắn xóa nhà cung cấp: {selected.Name} ?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            using var ctx = new FamiContext();
            var sup = ctx.Suppliers.Find(selected.SupplierId);
            if (sup != null)
            {
                ctx.Suppliers.Remove(sup);
                ctx.SaveChanges();
            }

            ClearForm();
            LoadSuppliers();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            dgSuppliers.SelectedItem = null;
        }

        private void dgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSuppliers.SelectedItem is Supplier selected)
            {
                txtName.Text = selected.Name;
                txtAddress.Text = selected.Address ?? string.Empty;
                txtPhoneNumber.Text = selected.PhoneNumber ?? string.Empty;
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
            txtPhoneNumber.Clear();
        }
    }
}
