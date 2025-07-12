using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Family_Business.Models;
using Microsoft.VisualBasic;

namespace Family_Business.Views
{
    public partial class SaleInvoiceView : UserControl
    {
        private readonly List<InvoiceLineViewModel> _lines = new();
        private readonly FamiContext _ctx = new();

        public SaleInvoiceView()
        {
            InitializeComponent();
            LoadHeaderLookups();
            RefreshLinesGrid();
        }

        private void LoadHeaderLookups()
        {
            cbCustomer.ItemsSource = _ctx.Customers.ToList();
            cbLineProduct.ItemsSource = _ctx.Products.Include(p => p.BaseUnit).ToList();
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);
        }

        private void CbLineProduct_SelectionChanged(object s, SelectionChangedEventArgs e)
            => RecalculateLinePrice();

        private void TbLineQuantity_TextChanged(object s, TextChangedEventArgs e)
            => RecalculateLinePrice();

        private void RecalculateLinePrice()
        {
            if (cbLineProduct.SelectedItem is not Product prod
                || !int.TryParse(tbLineQuantity.Text, out var qty))
            {
                lblUnitPrice.Text = lblLineTotal.Text = "";
                return;
            }

            // 1) Lấy CostPerUnit trực tiếp qua SQL từ bảng ProductUnit :contentReference[oaicite:0]{index=0}
            decimal? cost = GetCostPerUnit(prod.ProductId, prod.BaseUnitId);
            if (cost == null)
            {
                lblUnitPrice.Text = lblLineTotal.Text = "";
                return;
            }

            // 2) Tính đơn giá = cost * (1 + markup/100)
            var unitPrice = cost.Value * (1 + prod.RetailMarkupPercent / 100m);
            var lineTotal = unitPrice * qty;

            lblUnitPrice.Text = unitPrice.ToString("N2");
            lblLineTotal.Text = lineTotal.ToString("N2");
        }

        private decimal? GetCostPerUnit(int productId, int unitId)
        {
            using var conn = _ctx.Database.GetDbConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT CostPerUnit 
                  FROM ProductUnit 
                 WHERE ProductID = @pid AND UnitID = @uid";
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@pid"; p1.Value = productId;
            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@uid"; p2.Value = unitId;
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);

            if (conn.State != ConnectionState.Open)
                conn.Open();

            var result = cmd.ExecuteScalar();
            return result == null || result is DBNull
                ? null
                : (decimal?)Convert.ToDecimal(result);
        }
        private void BtnClearInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Xóa hết các dòng
            _lines.Clear();
            RefreshLinesGrid();

            // Reset header
            cbCustomer.SelectedIndex = -1;
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);

            // Reset dòng nhập
            cbLineProduct.SelectedIndex = -1;
            tbLineQuantity.Clear();
            lblUnitPrice.Text = "";
            lblLineTotal.Text = "";
        }

        private void BtnAddLine_Click(object s, RoutedEventArgs e)
        {
            if (cbLineProduct.SelectedItem is not Product prod
                || !int.TryParse(tbLineQuantity.Text, out var qty)
                || string.IsNullOrEmpty(lblLineTotal.Text))
            {
                MessageBox.Show(
                    "Chọn sản phẩm, nhập số lượng và đợi tính giá.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning
                );
                return;
            }

            _lines.Add(new InvoiceLineViewModel
            {
                Product = prod,
                Quantity = qty,
                UnitPrice = decimal.Parse(lblUnitPrice.Text),
                Total = decimal.Parse(lblLineTotal.Text)
            });
            RefreshLinesGrid();
            tbLineQuantity.Clear();
        }

        private void RefreshLinesGrid()
        {
            dgLines.ItemsSource = null;
            dgLines.ItemsSource = _lines;
            lblInvoiceTotal.Text = _lines.Sum(x => x.Total).ToString("N2");
        }

        private void BtnSaveInvoice_Click(object s, RoutedEventArgs e)
        {
            if (cbCustomer.SelectedItem is not Customer cust || !_lines.Any())
            {
                MessageBox.Show(
                    "Chọn khách hàng và thêm ít nhất một dòng.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning
                );
                return;
            }

            // Lưu Invoice
            var inv = new Invoice
            {
                CustomerId = cust.CustomerID,
                InvoiceDate = dpInvoiceDate.SelectedDate.Value,
                DueDate = dpDueDate.SelectedDate,
                CreatedBy = 1 /* UserID hiện tại */
            };
            _ctx.Invoices.Add(inv);
            _ctx.SaveChanges();

            // Lưu chi tiết
            foreach (var ln in _lines)
            {
                _ctx.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = inv.InvoiceId,
                    ProductId = ln.Product.ProductId,
                    UnitId = ln.Product.BaseUnitId,
                    Quantity = ln.Quantity,
                    UnitPrice = ln.UnitPrice
                });
            }
            _ctx.SaveChanges();

            MessageBox.Show(
                $"Hóa đơn #{inv.InvoiceId} đã lưu! Tổng: {lblInvoiceTotal.Text}",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information
            );

            // Reset
            _lines.Clear();
            RefreshLinesGrid();
            cbCustomer.SelectedIndex = -1;
            cbLineProduct.SelectedIndex = -1;
            tbLineQuantity.Clear();
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);
        }
    }

    public class InvoiceLineViewModel
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
