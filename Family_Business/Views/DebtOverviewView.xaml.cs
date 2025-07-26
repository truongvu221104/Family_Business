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
        public bool IsHighlight { get; set; } // mới thêm
        public bool IsOverdue { get; set; } // thêm mới
    
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
            dpFilterDueDate.DisplayDateStart = DateTime.Today.AddDays(1);
            LoadDebtors();
        }
        private void LoadDebtors()
        {
            var query = _ctx.Customers.AsQueryable();
            query = query.Where(c => c.Balance > 0);

            var key = txtFilterNameOrPhone.Text.Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                key = key.Replace(" ", "").ToLower();
                query = query.Where(c =>
                    (c.Name != null && c.Name.ToLower().Contains(key)) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Replace(" ", "").Contains(key))
                );
            }

            // 1. Lấy danh sách khách nợ cơ bản
            var customers = query
                .Select(c => new DebtorInfo
                {
                    CustomerID = c.CustomerID,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Balance = c.Balance
                })
                .ToList();

            // 2. Lấy hóa đơn gần nhất có DueDate cho từng khách
            foreach (var cust in customers)
            {
                var nextInvoice = _ctx.Invoices
                    .Where(i => i.CustomerId == cust.CustomerID && i.DueDate != null)
                    .OrderBy(i => i.DueDate)
                    .FirstOrDefault();

                cust.NextDueDate = nextInvoice?.DueDate;
                cust.NextDueInvoiceId = nextInvoice?.InvoiceId;
            }

            // 3. Nếu có chọn ngày đáo hạn, chỉ lọc khách có ngày đáo hạn trước ngày đó
            DateTime? filterDate = dpFilterDueDate.SelectedDate;
            DateTime baseDate = filterDate ?? DateTime.Today;
            if (filterDate.HasValue)
            {
                customers = customers
                    .Where(x => x.NextDueDate != null && x.NextDueDate < baseDate)
                    .ToList();
            }

            // 4. Đánh dấu highlight và overdue cho mọi trường hợp (luôn chạy, không phụ thuộc filter)
            DateTime startHighlight = baseDate.AddDays(-7);
            foreach (var item in customers)
            {
                item.IsHighlight = false;
                item.IsOverdue = false;
                if (item.NextDueDate.HasValue)
                {
                    if (item.NextDueDate.Value < baseDate)
                    {
                        if (item.NextDueDate.Value < baseDate.AddDays(-1))
                            item.IsOverdue = true; // đỏ
                        else
                            item.IsHighlight = true; // vàng
                    }
                    else if (item.NextDueDate.Value >= startHighlight && item.NextDueDate.Value < baseDate)
                    {
                        item.IsHighlight = true; // vàng
                    }
                }
            }

            // 5. Sắp xếp khách gần đến hạn nhất lên đầu
            customers = customers.OrderBy(x => x.NextDueDate ?? DateTime.MaxValue).ToList();

            _debtors = customers;
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

            if (_selectedCust != null)
            {
                var toSelect = _debtors.FirstOrDefault(d => d.CustomerID == _selectedCust.CustomerID);
                if (toSelect != null)
                    dgDebtors.SelectedItem = toSelect;
                else
                {
                    // Không còn trong danh sách => clear selection
                    dgDebtors.SelectedItem = null;
                    _selectedCust = null;
                    _selectedInvoiceId = null;
                    tbOwed.Clear();
                }
            }
        }
    }
}
