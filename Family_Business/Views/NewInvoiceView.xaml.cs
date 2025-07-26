using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Family_Business.Models;
using Family_Business.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public partial class NewInvoiceView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private readonly CollectionViewSource _customerView;
        private readonly CollectionViewSource _productView;
        private readonly NewInvoiceViewModel _vm;

        public NewInvoiceView()
        {
            InitializeComponent();

            // 1) Tạo & gán ViewModel
            _vm = new NewInvoiceViewModel();
            DataContext = _vm;

            // 2) Khởi tạo filter Khách
            _customerView = new CollectionViewSource { Source = _ctx.Customers.ToList() };
            _customerView.Filter += CustomerFilter;
            cbCustomer.ItemsSource = _customerView.View;

            // 3) Khởi tạo filter Sản phẩm
            _productView = new CollectionViewSource
            {
                Source = _ctx.Products.Include(p => p.BaseUnit).ToList()
            };
            _productView.Filter += ProductFilter;
            cbProduct.ItemsSource = _productView.View;

            // 4) Mặc định Quantity & Paid, rồi tính giá lẻ
            _vm.Quantity = 1;
            _vm.Paid = 0m;
        }

        // Filter Khách
        private void CustomerFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomerFilter.Text))
                e.Accepted = true;
            else if (e.Item is Customer c)
            {
                var key = txtCustomerFilter.Text.Trim().ToLower().Replace(" ", "");
                var name = c.Name?.ToLower() ?? "";
                var phone = (c.PhoneNumber ?? "").Replace(" ", "");
                e.Accepted = name.Contains(key) || phone.Contains(key);
            }
            else
                e.Accepted = false;
        }
        private void TxtCustomerFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _customerView.View.Refresh();

        // Filter Sản phẩm
        private void ProductFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductFilter.Text))
                e.Accepted = true;
            else if (e.Item is Product p)
                e.Accepted = p.Name.IndexOf(txtProductFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                e.Accepted = false;
        }
        private void TxtProductFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _productView.View.Refresh();

        // Khi chọn SP: hiện Đơn vị và tính lại Đơn giá, đồng thời hiển thị tồn kho
        private void CbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProduct.SelectedItem is Product prod)
            {
                lblUnitName.Text = prod.BaseUnit.UnitName;
                RecalculateUnitPrice(prod);

                // Tính tồn kho và hiển thị
                var inventory = _ctx.InventoryTransactions
                    .Where(tx => tx.ProductId == prod.ProductId)
                    .Sum(tx => tx.TxType == "Purchase" ? tx.Quantity
                            : (tx.TxType == "Sale" || tx.TxType == "OUT") ? -tx.Quantity
                            : 0);
                lblInventory.Text = inventory.ToString("0.##");
            }
            else
            {
                lblUnitName.Text = "";
                lblInventory.Text = "";
                _vm.UnitPrice = 0m;
            }
        }

        // Khi đổi Loại bán (Bán lẻ/Bán sỉ)
        private void SaleModeChanged(object sender, RoutedEventArgs e)
        {
            if (cbProduct.SelectedItem is Product prod)
                RecalculateUnitPrice(prod);
        }

        // Tính và gán Giá theo loại bán
        private void RecalculateUnitPrice(Product prod)
        {
            var cost = prod.CostPerUnit;
            var pct = (rbWholesale.IsChecked == true)
                         ? prod.WholesaleMarkupPercent
                         : prod.RetailMarkupPercent;
            _vm.UnitPrice = cost * (1 + pct / 100m);
        }

        // Nút "Thanh toán đủ"
        private void BtnPayFull_Click(object sender, RoutedEventArgs e)
        {
            _vm.Paid = _vm.Total;
            tbPaid.Text = _vm.Total.ToString("N2");
        }

        // Lưu hóa đơn + chi tiết
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1) Validate khách hàng
            if (!(cbCustomer.SelectedItem is Customer cust))
            {
                MessageBox.Show("Chọn khách hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // 2) Validate sản phẩm
            if (!(cbProduct.SelectedItem is Product prod))
            {
                MessageBox.Show("Chọn sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3) Lấy dữ liệu từ VM
            var qty = _vm.Quantity;
            var unitPrice = _vm.UnitPrice;
            var paid = _vm.Paid;
            var total = _vm.Total;
            var now = DateTime.Now;
            int currentUserId = 1;

            // 4) Kiểm tra số lượng bán là số nguyên dương
            if (qty <= 0 || qty % 1 != 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên dương.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5) Kiểm tra tồn kho
            var inventory = _ctx.InventoryTransactions
                .Where(tx => tx.ProductId == prod.ProductId)
                .Sum(tx => tx.TxType == "Purchase" ? tx.Quantity
                        : (tx.TxType == "Sale" || tx.TxType == "OUT") ? -tx.Quantity
                        : 0);

            if (qty > inventory)
            {
                MessageBox.Show($"Số lượng tồn kho hiện tại chỉ còn {inventory:0.##}. Không đủ số lượng để bán!", "Lỗi tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 6) Kiểm tra số tiền khách trả
            if (paid > total)
            {
                MessageBox.Show("Số tiền khách trả không được lớn hơn thành tiền!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 7) Nếu khách còn nợ thì phải chọn ngày đáo hạn hợp lệ
            if (paid < total)
            {
                if (!_vm.DueDate.HasValue)
                {
                    MessageBox.Show("Khách nợ, vui lòng chọn ngày đáo hạn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_vm.DueDate.Value.Date <= _vm.InvoiceDateTime.Date)
                {
                    MessageBox.Show("Ngày đáo hạn phải lớn hơn ngày lập hóa đơn.", "Lỗi ngày đáo hạn", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            using var tx = _ctx.Database.BeginTransaction();
            try
            {
                // 8) Tạo hóa đơn
                var inv = new Invoice
                {
                    CustomerId = cust.CustomerID,
                    InvoiceDate = _vm.InvoiceDateTime,
                    DueDate = (paid < total) ? _vm.DueDate : null,
                    CreatedBy = currentUserId
                };
                _ctx.Invoices.Add(inv);
                _ctx.SaveChanges();

                // 9) Thêm chi tiết hóa đơn
                _ctx.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = inv.InvoiceId,
                    ProductId = prod.ProductId,
                    UnitId = prod.BaseUnitId,
                    Quantity = qty,
                    UnitPrice = unitPrice
                });

                // 10) Ghi giao dịch xuất kho
                _ctx.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = prod.ProductId,
                    UnitId = prod.BaseUnitId,
                    Quantity = qty,
                    TxType = "Sale",
                    PartyId = cust.CustomerID,
                    PartyType = "Customer",
                    ReferenceId = inv.InvoiceId,
                    TxDate = _vm.InvoiceDateTime,
                    Note = "Xuất kho bán hàng"
                });

                // 11) Thêm payment nếu có
                if (paid > 0)
                {
                    _ctx.Payments.Add(new Payment
                    {
                        InvoiceId = inv.InvoiceId,
                        Amount = paid,
                        PaymentDate = now,
                        Type = "Thu",
                        CreatedBy = currentUserId
                    });
                }

                // 12) Ghi log
                _ctx.AuditLogs.Add(new AuditLog
                {
                    UserId = currentUserId,
                    Action = "Create Invoice",
                    TableName = "Invoice",
                    RecordId = inv.InvoiceId,
                    ActionTime = now,
                    Detail = $"Total={total:N2}; Paid={paid:N2}"
                });

                _ctx.SaveChanges();

                // 13) Tính số còn nợ của chính hóa đơn này
                decimal outstandingThisInvoice = total - paid;
                // Đảm bảo không có nợ âm cho hóa đơn này
                if (outstandingThisInvoice < 0) outstandingThisInvoice = 0;

                // 14) Cộng dồn vào Balance cũ rồi lưu
                cust.Balance += outstandingThisInvoice;
                _ctx.SaveChanges();

                tx.Commit();

                MessageBox.Show(
                    $"Hóa đơn #{inv.InvoiceId} đã lưu.\n" +
                    $"Số còn nợ của khách ({cust.Name}): {cust.Balance:N2}",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                ResetForm();

                // Sau khi bán xong, nếu đang chọn sản phẩm thì cập nhật lại số tồn kho hiển thị
                if (cbProduct.SelectedItem != null)
                    CbProduct_SelectionChanged(null, null);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show($"Lỗi khi lưu hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Nút Hủy và reset form
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        // Tách riêng phần reset để gọn
        private void ResetForm()
        {
            txtCustomerFilter.Clear();
            cbCustomer.SelectedIndex = -1;
            txtProductFilter.Clear();
            cbProduct.SelectedIndex = -1;
            lblUnitName.Text = "";
            lblInventory.Text = "";
            rbRetail.IsChecked = true;
            _vm.Quantity = 1;
            _vm.Paid = 0m;
            _vm.DueDate = null;
            _vm.UnitPrice = 0m;
            // InvoiceDateTime giữ mặc định lần mở form; nếu cần reset, có thể gán lại DateTime.Now
        }
    }
}
