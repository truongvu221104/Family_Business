using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public class TransactionInfo
    {
        public string TransactionType { get; set; } = "";
        public DateTime Date { get; set; }
        public string PartyName { get; set; } = "";
        public string ProductDetail { get; set; } = "";
        public decimal Amount { get; set; }
        public string Note { get; set; } = "";
    }

    public partial class TransactionHistoryView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private List<TransactionInfo> _allTransactions = new();
        private List<TransactionInfo> _displayed = new();

        public TransactionHistoryView()
        {
            InitializeComponent();
            cbTypeFilter.SelectedIndex = 0; // Mặc định "Tất cả"
            LoadTransactions();
            cbMonth.Items.Add("Tất cả");
            for (int m = 1; m <= 12; m++) cbMonth.Items.Add(m.ToString("D2"));
            cbMonth.SelectedIndex = 0;

            cbYear.Items.Add("Tất cả");
            int yearNow = DateTime.Now.Year;
            for (int y = yearNow - 5; y <= yearNow; y++) cbYear.Items.Add(y.ToString());
            cbYear.SelectedIndex = cbYear.Items.Count - 1; // Default là năm hiện tại

            cbTypeFilter.SelectedIndex = 0;
            LoadTransactions();
        }

        private void LoadTransactions()
        {
            var sales = _ctx.Invoices
     .Include(i => i.Customer)
     .Include(i => i.InvoiceDetails)
     .ThenInclude(d => d.Product)
     .SelectMany(i => i.InvoiceDetails.Select(d => new TransactionInfo
     {
         TransactionType = "Bán hàng",
         Date = i.InvoiceDate,
         PartyName = i.Customer.Name,
         ProductDetail = $"{d.Product.Name} x{d.Quantity} ({d.UnitPrice:N2})",
         Amount = (d.Total ?? d.Quantity * d.UnitPrice),
         Note = ""
     }))
     .ToList();

            var payments = _ctx.Payments
                .Include(p => p.Invoice).ThenInclude(inv => inv.Customer)
                .Where(p => p.Type == "Thu" && p.Invoice != null)
                .Select(p => new TransactionInfo
                {
                    TransactionType = "Thanh toán",
                    Date = p.PaymentDate,
                    PartyName = p.Invoice.Customer.Name,
                    ProductDetail = $"Thanh toán hóa đơn #{p.InvoiceId}",
                    Amount = p.Amount,
                    Note = p.Note ?? ""
                })
                .ToList();

            var purchases = _ctx.Payments
                .Include(p => p.Supplier)
                .Where(p => p.Type == "Chi" && p.Supplier != null)
                .Select(p => new TransactionInfo
                {
                    TransactionType = "Nhập hàng",
                    Date = p.PaymentDate,
                    PartyName = p.Supplier.Name,
                    ProductDetail = $"Thanh toán NCC",
                    Amount = p.Amount,
                    Note = p.Note ?? ""
                })
                .ToList();

            var warehouse = _ctx.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Unit)
                .Select(t => new TransactionInfo
                {
                    TransactionType = t.TxType == "OUT" ? "Xuất kho" : "Nhập kho",
                    Date = t.TxDate,
                    PartyName = t.PartyType == "Customer" ? ("KH #" + t.PartyId) :
                                t.PartyType == "Supplier" ? ("NCC #" + t.PartyId) : "",
                    ProductDetail = $"{t.Product.Name} x{t.Quantity} {t.Unit.UnitName}",
                    Amount = t.Quantity * t.Product.CostPerUnit,
                    Note = t.Note ?? ""
                })
                .ToList();

            _allTransactions = sales
                .Concat(payments)
                .Concat(purchases)
                .Concat(warehouse)
                .OrderByDescending(t => t.Date)
                .ToList();
            ApplyFilters();
        }

        // Lọc theo loại GD, từ khóa
        private void ApplyFilters()
        {
            string keyword = tbSearch.Text.Trim().ToLower();
            string type = (cbTypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả";

            // Lọc ngày
            DateTime? from = dpFrom.SelectedDate;
            DateTime? to = dpTo.SelectedDate;

            // Lọc tháng
            bool filterMonth = cbMonth.SelectedIndex > 0;
            int month = filterMonth ? int.Parse(cbMonth.SelectedItem.ToString()) : 0;

            // Lọc năm
            bool filterYear = cbYear.SelectedIndex > 0;
            int year = filterYear ? int.Parse(cbYear.SelectedItem.ToString()) : 0;

            _displayed = _allTransactions
                .Where(t =>
                    (type == "Tất cả" || t.TransactionType == type) &&
                    (string.IsNullOrWhiteSpace(keyword) ||
                        (t.PartyName?.ToLower().Contains(keyword) ?? false) ||
                        (t.ProductDetail?.ToLower().Contains(keyword) ?? false)) &&
                    (!from.HasValue || t.Date.Date >= from.Value.Date) &&
                    (!to.HasValue || t.Date.Date <= to.Value.Date) &&
                    (!filterMonth || t.Date.Month == month) &&
                    (!filterYear || t.Date.Year == year)
                )
                .OrderByDescending(t => t.Date)
                .ToList();

            dgTransactions.ItemsSource = _displayed;

            // Tổng hợp Thu (Thanh toán) và Chi (Nhập hàng)
            decimal totalBanHang = _displayed.Where(t => t.TransactionType == "Bán hàng").Sum(t => t.Amount);
            decimal totalThu = _displayed.Where(t => t.TransactionType == "Thanh toán").Sum(t => t.Amount);
            decimal totalChi = _displayed.Where(t => t.TransactionType == "Nhập hàng").Sum(t => t.Amount);


            if (type == "Tất cả")
            {
                lblTotalBanHang.Text = totalBanHang.ToString("N0") + " đ";
                lblTotalThu.Text = totalThu.ToString("N0") + " đ";
                lblTotalChi.Text = totalChi.ToString("N0") + " đ";
            }
            else if (type == "Bán hàng")
            {
                lblTotalBanHang.Text = totalBanHang.ToString("N0") + " đ";
                lblTotalThu.Text = "—";
                lblTotalChi.Text = "—";
            }
            else if (type == "Thanh toán")
            {
                lblTotalBanHang.Text = "—";
                lblTotalThu.Text = totalThu.ToString("N0") + " đ";
                lblTotalChi.Text = "—";
            }
            else if (type == "Nhập hàng")
            {
                lblTotalBanHang.Text = "—";
                lblTotalThu.Text = "—";
                lblTotalChi.Text = totalChi.ToString("N0") + " đ";
            }
            else
            {
                lblTotalBanHang.Text = "—";
                lblTotalThu.Text = "—";
                lblTotalChi.Text = "—";
            }
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void DateFilter_Changed(object sender, EventArgs e) => ApplyFilters();
    }
}
