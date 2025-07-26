using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class InventoryTransactionListView : UserControl
    {
        private readonly FamiContext _ctx = new();
        private readonly string[] _types = { "All", "Purchase", "Sale", "Return" };

        public InventoryTransactionListView()
        {
            InitializeComponent();
            LoadFilters();
            LoadTransactions();
        }

        private void LoadFilters()
        {
            // Loại giao dịch
            cbFilterType.ItemsSource = _types;
            cbFilterType.SelectedIndex = 0;

            // Sản phẩm
            var products = _ctx.Products
                               .OrderBy(p => p.Name)
                               .ToList();
            cbFilterProduct.ItemsSource = products;
            cbFilterProduct.SelectedIndex = -1;

            // Ngày mặc định: 1 tháng gần nhất
            dpFrom.SelectedDate = DateTime.Today.AddMonths(-1);
            dpTo.SelectedDate = DateTime.Today;
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
            => LoadTransactions();

        // Gọi chung khi filter thay đổi
        private void Filter_Changed(object sender, EventArgs e)
            => LoadTransactions();

        private void LoadTransactions()
        {
            // Validate ngày lọc trước
            if (dpFrom.SelectedDate.HasValue && dpTo.SelectedDate.HasValue &&
                dpFrom.SelectedDate > dpTo.SelectedDate)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Lỗi lọc", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuẩn bị query
            var q = _ctx.InventoryTransactions
                        .Include(t => t.Product)
                        .Include(t => t.Unit)
                        .AsQueryable();

            // Lọc loại
            var selType = cbFilterType.SelectedItem as string;
            if (!string.IsNullOrEmpty(selType) && selType != "All")
                q = q.Where(t => t.TxType == selType);

            // Lọc sản phẩm
            if (cbFilterProduct.SelectedItem is Product p)
                q = q.Where(t => t.ProductId == p.ProductId);

            // Lọc ngày
            if (dpFrom.SelectedDate.HasValue)
                q = q.Where(t => t.TxDate >= dpFrom.SelectedDate.Value);
            if (dpTo.SelectedDate.HasValue)
                q = q.Where(t => t.TxDate < dpTo.SelectedDate.Value.AddDays(1));

            // Đổ về list để bind
            var list = q.OrderByDescending(t => t.TxDate)
                        .Select(t => new InventoryTxDto
                        {
                            TxId = t.TxId,
                            TxDate = t.TxDate,
                            TxType = t.TxType,
                            ProductName = t.Product.Name,
                            UnitName = t.Unit.UnitName,
                            Quantity = t.Quantity,
                            PartyName = t.PartyType == "Customer"
                                            ? _ctx.Customers
                                                   .Where(c => c.CustomerID == t.PartyId)
                                                   .Select(c => c.Name)
                                                   .FirstOrDefault()
                                            : _ctx.Suppliers
                                                   .Where(s => s.SupplierId == t.PartyId)
                                                   .Select(s => s.Name)
                                                   .FirstOrDefault(),
                            Note = t.Note
                        })
                        .ToList();

            dgTransactions.ItemsSource = list;
        }

        // DTO để binding
        private class InventoryTxDto
        {
            public int TxId { get; set; }
            public DateTime TxDate { get; set; }
            public string TxType { get; set; }
            public string ProductName { get; set; }
            public string UnitName { get; set; }
            public decimal Quantity { get; set; }
            public string PartyName { get; set; }
            public string? Note { get; set; }
        }
    }
}
