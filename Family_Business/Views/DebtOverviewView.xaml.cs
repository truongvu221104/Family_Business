using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;

namespace Family_Business.Views
{
    // DTO để bind DataGrid
    public class DebtorInfo
    {
        public int CustomerID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int? NextDueInvoiceId { get; set; }
    }

    public partial class DebtOverviewView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private List<DebtorInfo> _debtors = new();
        private Customer? _selectedCust;
        private int? _selectedInvoiceId;

        public DebtOverviewView()
        {
            InitializeComponent();
            LoadDebtors();
        }

        private void LoadDebtors()
        {
            var query = _ctx.Customers.AsQueryable();
            // chỉ khách còn nợ
            query = query.Where(c => c.Balance > 0);

            // lọc theo tên hoặc số điện thoại
            var key = txtFilterNameOrPhone.Text.Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                query = query.Where(c => c.Name.Contains(key) || c.PhoneNumber.Contains(key));
            }

            // lấy danh sách
            var list = query
                .Select(c => new DebtorInfo
                {
                    CustomerID = c.CustomerID,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Balance = c.Balance,
                    NextDueDate = _ctx.Invoices
                                          .Where(i => i.CustomerId == c.CustomerID && i.DueDate != null)
                                          .OrderBy(i => i.DueDate)
                                          .Select(i => i.DueDate)
                                          .FirstOrDefault(),
                    NextDueInvoiceId = _ctx.Invoices
                                          .Where(i => i.CustomerId == c.CustomerID && i.DueDate != null)
                                          .OrderBy(i => i.DueDate)
                                          .Select(i => i.InvoiceId)
                                          .FirstOrDefault()
                })
                .ToList();

            // tiếp tục lọc theo ngày đáo hạn
            if (dpFilterDueDate.SelectedDate is DateTime d)
            {
                list = list.Where(x => x.NextDueDate >= d).ToList();
            }

            _debtors = list;
            dgDebtors.ItemsSource = _debtors;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            LoadDebtors();
            tbPayment.Clear();
            tbOwed.Clear();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtFilterNameOrPhone.Clear();
            dpFilterDueDate.SelectedDate = null;
            LoadDebtors();
        }

        private void DgDebtors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDebtors.SelectedItem is DebtorInfo row)
            {
                _selectedCust = _ctx.Customers.Find(row.CustomerID);
                _selectedInvoiceId = row.NextDueInvoiceId;
                tbOwed.Text = row.Balance.ToString("N2");
            }
            else
            {
                _selectedCust = null;
                _selectedInvoiceId = null;
                tbOwed.Clear();
            }
        }

        private void BtnPay_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCust == null ||
                _selectedInvoiceId == null ||
                !decimal.TryParse(tbPayment.Text, out var paid) || paid <= 0)
            {
                MessageBox.Show("Chọn khách, hóa đơn và nhập số tiền > 0.", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var now = DateTime.Now;
            // 1) Ghi Payment liên kết với hóa đơn
            var payment = new Payment
            {
                InvoiceId = _selectedInvoiceId,
                Amount = paid,
                PaymentDate = now,
                Type = "Thu",
                CreatedBy = 1
            };
            _ctx.Payments.Add(payment);

            // 2) Cập nhật Balance của khách
            _selectedCust.Balance -= paid;
            if (_selectedCust.Balance < 0) _selectedCust.Balance = 0;

            _ctx.SaveChanges();

            // 3) Ghi AuditLog
            var audit = new AuditLog
            {
                UserId = 1,
                Action = "Receive Payment",
                TableName = "Payment",
                RecordId = payment.PaymentId,
                ActionTime = now,
                Detail = $"CustomerID={_selectedCust.CustomerID}; Amount={paid:N2}; PaidOn={now:yyyy-MM-dd HH:mm:ss}"
            };
            _ctx.AuditLogs.Add(audit);
            _ctx.SaveChanges();

            // 4) Hiển thị kết quả và reload
            tbOwed.Text = _selectedCust.Balance.ToString("N2");
            MessageBox.Show($"Thanh toán {paid:N2} thành công.\nCòn nợ: {_selectedCust.Balance:N2}",
                            "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reload giữ selection
            LoadDebtors();
            tbPayment.Clear();
            var toSelect = _debtors.FirstOrDefault(d => d.CustomerID == _selectedCust.CustomerID);
            if (toSelect != null) dgDebtors.SelectedItem = toSelect;
        }
    }
}
