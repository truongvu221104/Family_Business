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
            if (_selectedSup == null ||
                !decimal.TryParse(tbPayment.Text, out var paid) || paid <= 0)
            {
                MessageBox.Show("Chọn nhà cung cấp và nhập số tiền > 0.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lưu supplierId trước khi reload
            var supplierId = _selectedSup.SupplierId;
            var now = DateTime.Now;

            // 1) Ghi Payment
            var payment = new Payment
            {
                SupplierId = supplierId,
                Amount = paid,
                PaymentDate = now,
                Type = "Chi",
                CreatedBy = 1
            };
            _ctx.Payments.Add(payment);

            // 2) Cập nhật OutstandingPayable
            _selectedSup.OutstandingPayable = Math.Max(0, _selectedSup.OutstandingPayable - paid);
            _ctx.SaveChanges();

            // 3) Ghi AuditLog
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

            // 4) Hiển thị kết quả
            tbOwed.Text = _selectedSup.OutstandingPayable.ToString("N2");
            MessageBox.Show($"Đã trả {paid:N2}. Còn nợ: {_selectedSup.OutstandingPayable:N2}",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            // 5) Reload và giữ selection
            LoadSuppliersDebt();
            tbPayment.Clear();
            var toSel = _suppliers.FirstOrDefault(s => s.SupplierID == supplierId);
            if (toSel != null)
                dgSuppliers.SelectedItem = toSel;
        }
    }
}
