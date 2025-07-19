using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public partial class NewInvoiceView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private readonly CollectionViewSource _customerView;
        private readonly CollectionViewSource _productView;

        public NewInvoiceView()
        {
            InitializeComponent();

            // 1) Thiết lập filter khách
            _customerView = new CollectionViewSource { Source = _ctx.Customers.ToList() };
            _customerView.Filter += CustomerFilter;
            cbCustomer.ItemsSource = _customerView.View;

            // 2) Thiết lập filter sản phẩm
            _productView = new CollectionViewSource
            {
                Source = _ctx.Products.Include(p => p.BaseUnit).ToList()
            };
            _productView.Filter += ProductFilter;
            cbProduct.ItemsSource = _productView.View;

            // 3) Đăng ký sự kiện nhập đơn giá, số lượng sau khi khởi tạo control
            tbUnitPrice.TextChanged += TbLineInput_TextChanged;
            tbQuantity.TextChanged += TbLineInput_TextChanged;

            // 4) Khởi tạo mặc định
            tbQuantity.Text = "1";
            tbUnitPrice.Text = "0";
            lblLineTotal.Text = "";
            tbPaid.Text = "";
            lblStatus.Text = "";
        }

        // Filter khách
        private void CustomerFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomerFilter.Text))
                e.Accepted = true;
            else if (e.Item is Customer c)
                e.Accepted = c.Name.IndexOf(txtCustomerFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                e.Accepted = false;
        }

        // Filter sản phẩm
        private void ProductFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductFilter.Text))
                e.Accepted = true;
            else if (e.Item is Product p)
                e.Accepted = p.Name.IndexOf(txtProductFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                e.Accepted = false;
        }

        private void TxtCustomerFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _customerView.View.Refresh();

        private void TxtProductFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _productView.View.Refresh();

        // Khi chọn sản phẩm → hiển thị tên đơn vị
        private void CbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProduct.SelectedItem is Product prod)
            {
                lblUnitName.Text = prod.BaseUnit.UnitName;
                RecalculateLineTotal();
            }
            else
            {
                lblUnitName.Text = "";
                lblLineTotal.Text = "";
            }
        }

        // Khi nhập đơn giá hoặc số lượng → tính thành tiền
        private void TbLineInput_TextChanged(object sender, TextChangedEventArgs e)
            => RecalculateLineTotal();

        private void RecalculateLineTotal()
        {
            if (decimal.TryParse(tbUnitPrice.Text, out var unitPrice)
                && int.TryParse(tbQuantity.Text, out var qty))
            {
                var total = unitPrice * qty;
                lblLineTotal.Text = total.ToString("N2");

                // Reset trạng thái trả tiền
                lblStatus.Text = "";
                tbPaid.Text = "";
            }
            else
            {
                lblLineTotal.Text = "";
            }
        }

        // Khi nhập số tiền khách trả → hiển thị trạng thái
        private void TbPaid_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(lblLineTotal.Text, out var total)
                && decimal.TryParse(tbPaid.Text, out var paid))
            {
                lblStatus.Text = paid >= total
                    ? "Đã thanh toán đủ"
                    : $"Còn nợ: {(total - paid):N2}";
            }
            else
            {
                lblStatus.Text = "";
            }
        }

        // Lưu hóa đơn + chi tiết
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!(cbCustomer.SelectedItem is Customer cust))
            {
                MessageBox.Show("Vui lòng chọn khách hàng.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(cbProduct.SelectedItem is Product prod)
                || !decimal.TryParse(tbUnitPrice.Text, out var unitPrice)
                || !int.TryParse(tbQuantity.Text, out var qty))
            {
                MessageBox.Show("Vui lòng chọn sản phẩm, nhập đơn giá và số lượng hợp lệ.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(tbPaid.Text, out var paid))
            {
                MessageBox.Show("Vui lòng nhập số tiền khách trả.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1) Tạo Invoice
            var inv = new Invoice
            {
                CustomerId = cust.CustomerID,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                CreatedBy = 1
            };
            _ctx.Invoices.Add(inv);
            _ctx.SaveChanges();

            // 2) Tạo InvoiceDetail
            _ctx.InvoiceDetails.Add(new InvoiceDetail
            {
                InvoiceId = inv.InvoiceId,
                ProductId = prod.ProductId,
                UnitId = prod.BaseUnitId,
                Quantity = qty,
                UnitPrice = unitPrice
            });
            _ctx.SaveChanges();

            // 3) Thông báo kết quả
            var status = paid >= unitPrice * qty
                ? "Đã thanh toán đủ."
                : $"Còn nợ: {(unitPrice * qty - paid):N2}.";
            MessageBox.Show(
                $"Hoá đơn #{inv.InvoiceId} đã lưu.\n{status}",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            // 4) Reset form
            txtCustomerFilter.Clear(); cbCustomer.SelectedIndex = -1;
            txtProductFilter.Clear(); cbProduct.SelectedIndex = -1;
            lblUnitName.Text = "";
            tbUnitPrice.Text = "0";
            tbQuantity.Text = "1";
            lblLineTotal.Text = "";
            tbPaid.Text = "";
            lblStatus.Text = "";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Reset form
            txtCustomerFilter.Clear(); cbCustomer.SelectedIndex = -1;
            txtProductFilter.Clear(); cbProduct.SelectedIndex = -1;
            lblUnitName.Text = "";
            tbUnitPrice.Text = "0";
            tbQuantity.Text = "1";
            lblLineTotal.Text = "";
            tbPaid.Text = "";
            lblStatus.Text = "";
        }
    }
}
