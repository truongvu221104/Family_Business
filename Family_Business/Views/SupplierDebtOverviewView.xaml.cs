using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    public class SupplierDebtInfo
    {
        public int SupplierID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal OutstandingPayable { get; set; }
    }

    public partial class SupplierDebtOverviewView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private List<SupplierDebtInfo> _suppliers = new();
        private Supplier? _selectedSup;

        public SupplierDebtOverviewView()
        {
            InitializeComponent();
            LoadSuppliersDebt();
        }

        private void LoadSuppliersDebt()
        {
            var key = txtFilterNameOrPhone.Text.Trim();
            var query = _ctx.Suppliers.AsQueryable()
                                     .Where(s => s.OutstandingPayable > 0);
            if (!string.IsNullOrWhiteSpace(key))
            {
                query = query.Where(s => s.Name.Contains(key) || s.PhoneNumber.Contains(key));
            }

            _suppliers = query
                .Select(s => new SupplierDebtInfo
                {
                    SupplierID = s.SupplierId,
                    Name = s.Name,
                    PhoneNumber = s.PhoneNumber,
                    Address = s.Address,
                    OutstandingPayable = s.OutstandingPayable
                })
                .ToList();

            dgSuppliers.ItemsSource = _suppliers;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            LoadSuppliersDebt();
            tbPayment.Clear();
            tbOwed.Clear();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtFilterNameOrPhone.Clear();
            LoadSuppliersDebt();
            tbPayment.Clear();
            tbOwed.Clear();
        }

        private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSuppliers.SelectedItem is SupplierDebtInfo row)
            {
                _selectedSup = _ctx.Suppliers.Find(row.SupplierID);
                tbOwed.Text = row.OutstandingPayable.ToString("N2");
            }
            else
            {
                _selectedSup = null;
                tbOwed.Clear();
            }
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate nhà cung cấp đã chọn
            if (_selectedSup == null)
            {
                MessageBox.Show("Vui lòng chọn nhà cung cấp.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validate nhập số tiền
            if (!decimal.TryParse(tbPayment.Text, out var paid))
            {
                MessageBox.Show("Vui lòng nhập số tiền hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbPayment.Focus();
                return;
            }

            if (paid <= 0)
            {
                MessageBox.Show("Số tiền trả phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbPayment.Focus();
                return;
            }

            // 3. Không cho trả quá số nợ
            if (paid > _selectedSup.OutstandingPayable)
            {
                MessageBox.Show($"Số tiền trả vượt quá số nợ hiện tại ({_selectedSup.OutstandingPayable:N2}).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbPayment.Focus();
                return;
            }

            // 4. Thực hiện thanh toán
            var supplierId = _selectedSup.SupplierId;
            var now = DateTime.Now;

            var payment = new Payment
            {
                SupplierId = supplierId,
                Amount = paid,
                PaymentDate = now,
                Type = "Chi",
                CreatedBy = 1
            };
            _ctx.Payments.Add(payment);

            _selectedSup.OutstandingPayable -= paid;
            if (_selectedSup.OutstandingPayable < 0)
                _selectedSup.OutstandingPayable = 0;

            _ctx.SaveChanges();

            _ctx.AuditLogs.Add(new AuditLog
            {
                UserId = 1,
                Action = "Pay Supplier",
                TableName = "Payment",
                RecordId = payment.PaymentId,
                ActionTime = now,
                Detail = $"SupplierID={supplierId}; Amount={paid:N2}; PaidOn={now:yyyy-MM-dd HH:mm:ss}"
            });
            _ctx.SaveChanges();

            tbOwed.Text = _selectedSup.OutstandingPayable.ToString("N2");
            MessageBox.Show($"Đã trả {paid:N2}. Còn nợ: {_selectedSup.OutstandingPayable:N2}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadSuppliersDebt();
            tbPayment.Clear();
            var toSel = _suppliers.FirstOrDefault(s => s.SupplierID == supplierId);
            if (toSel != null)
                dgSuppliers.SelectedItem = toSel;
        }
        private void tbPayment_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(((TextBox)sender).Text + e.Text, out _);
        }

    }

}
