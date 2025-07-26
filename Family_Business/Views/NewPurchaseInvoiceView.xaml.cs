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
    public partial class NewPurchaseInvoiceView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private readonly CollectionViewSource _supplierView;
        private readonly CollectionViewSource _productView;
        private readonly PurchaseInvoiceViewModel _vm;

        public NewPurchaseInvoiceView()
        {
            InitializeComponent();

            // 1) Tạo & gán ViewModel trong code-behind
            _vm = new PurchaseInvoiceViewModel();
            DataContext = _vm;

            // 2) Khởi tạo filter nhà cung cấp
            _supplierView = new CollectionViewSource { Source = _ctx.Suppliers.ToList() };
            _supplierView.Filter += SupplierFilter;
            cbSupplier.ItemsSource = _supplierView.View;

            // 3) Khởi tạo filter sản phẩm
            _productView = new CollectionViewSource
            {
                Source = _ctx.Products.Include(p => p.BaseUnit).ToList()
            };
            _productView.Filter += ProductFilter;
            cbProduct.ItemsSource = _productView.View;

            // 4) Giá trị mặc định
            _vm.Quantity = 1;
            _vm.Paid = 0m;
        }

        // --- Filter Nhà cung cấp ---
        private void SupplierFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSupplierFilter.Text))
                e.Accepted = true;
            else if (e.Item is Supplier s)
                e.Accepted = s.Name
                    .IndexOf(txtSupplierFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                e.Accepted = false;
        }
        private void TxtSupplierFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _supplierView.View.Refresh();

        // --- Filter Sản phẩm ---
        private void ProductFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductFilter.Text))
                e.Accepted = true;
            else if (e.Item is Product p)
                e.Accepted = p.Name
                    .IndexOf(txtProductFilter.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            else
                e.Accepted = false;
        }
        private void TxtProductFilter_TextChanged(object sender, TextChangedEventArgs e)
            => _productView.View.Refresh();

        // Khi chọn SP: hiện Đơn vị và tính giá nhập
        private void CbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProduct.SelectedItem is Product prod)
            {
                lblUnitName.Text = prod.BaseUnit.UnitName;
                _vm.UnitCost = prod.CostPerUnit;
            }
            else
            {
                lblUnitName.Text = "";
                _vm.UnitCost = 0m;
            }
        }

        // Xử lý Lưu Phiếu nhập
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate nhà cung cấp
            if (!(cbSupplier.SelectedItem is Supplier sup))
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validate sản phẩm
            if (!(cbProduct.SelectedItem is Product prod))
            {
                MessageBox.Show("Vui lòng chọn sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Validate số lượng (chỉ nhận số nguyên dương)
            if (_vm.Quantity <= 0 || _vm.Quantity != Math.Round((double)_vm.Quantity, 0))
            {
                MessageBox.Show("Số lượng phải là số nguyên dương.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 4. Validate giá nhập (phải > 0)
            if (_vm.UnitCost <= 0)
            {
                MessageBox.Show("Giá nhập phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 5. Lấy giá trị nhập
            var qty = _vm.Quantity;
            var cost = _vm.UnitCost;
            var paid = _vm.Paid;
            var total = _vm.Total;
            var now = DateTime.Now;
            int currentUserId = 1; // Hoặc lấy từ context đăng nhập

            // 6. Validate tiền thanh toán
            if (paid < 0)
            {
                MessageBox.Show("Số tiền thanh toán không được âm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (paid > total)
            {
                MessageBox.Show("Số tiền thanh toán không được lớn hơn thành tiền.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 7. Nếu là công nợ thì validate ngày đáo hạn
            if (_vm.IsDebt)
            {
                if (!_vm.DueDate.HasValue)
                {
                    MessageBox.Show("Vui lòng nhập ngày đáo hạn khi công nợ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_vm.DueDate.Value.Date <= _vm.InvoiceDateTime.Date)
                {
                    MessageBox.Show("Ngày đáo hạn phải lớn hơn ngày nhập hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 8. Ghi dữ liệu vào DB (bắt đầu transaction)
            using var tx = _ctx.Database.BeginTransaction();
            try
            {
                // a) Thêm giao dịch kho (Purchase)
                var invTx = new InventoryTransaction
                {
                    ProductId = prod.ProductId,
                    UnitId = prod.BaseUnitId,
                    Quantity = qty,
                    TxType = "Purchase",
                    PartyId = sup.SupplierId,
                    PartyType = "Supplier",
                    ReferenceId = null,
                    TxDate = _vm.InvoiceDateTime
                };
                _ctx.InventoryTransactions.Add(invTx);

                // b) Thêm thanh toán (nếu có)
                if (paid > 0)
                {
                    _ctx.Payments.Add(new Payment
                    {
                        SupplierId = sup.SupplierId,
                        InvoiceId = null,
                        Amount = paid,
                        PaymentDate = now,
                        Type = "Chi",
                        CreatedBy = currentUserId
                    });
                }

                // c) Ghi AuditLog
                _ctx.AuditLogs.Add(new AuditLog
                {
                    UserId = currentUserId,
                    Action = "Create Purchase",
                    TableName = "InventoryTransaction",
                    RecordId = invTx.TxId, // Lưu ý: sẽ có giá trị đúng sau SaveChanges
                    ActionTime = now,
                    Detail = $"Total={total:N2}; Paid={paid:N2}"
                });

                _ctx.SaveChanges();

                // d) Cập nhật OutstandingPayable của NCC sau khi SaveChanges
                var supplierId = sup.SupplierId;
                var totalPurchase = _ctx.InventoryTransactions
                    .Where(tx => tx.PartyType == "Supplier" && tx.PartyId == supplierId && tx.TxType == "Purchase")
                    .Sum(tx => tx.Quantity * tx.Product.CostPerUnit);

                var totalPaid = _ctx.Payments
                    .Where(p => p.SupplierId == supplierId)
                    .Sum(p => (decimal?)p.Amount) ?? 0m;

                var newOutstanding = totalPurchase - totalPaid;

                var supplier = _ctx.Suppliers.Find(supplierId);
                supplier!.OutstandingPayable = newOutstanding;
                _ctx.SaveChanges();

                tx.Commit();

                MessageBox.Show($"Phiếu nhập đã lưu thành công!\nTrạng thái: {_vm.Status}",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                ResetForm();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show($"Lỗi khi lưu phiếu nhập: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnCancel_Click(object sender, RoutedEventArgs e) => ResetForm();

        private void ResetForm()
        {
            txtSupplierFilter.Clear(); cbSupplier.SelectedIndex = -1;
            txtProductFilter.Clear(); cbProduct.SelectedIndex = -1;
            lblUnitName.Text = "";
            _vm.Quantity = 1;
            _vm.Paid = 0m;
            _vm.DueDate = null;
            _vm.UnitCost = 0m;
        }
    }
}
