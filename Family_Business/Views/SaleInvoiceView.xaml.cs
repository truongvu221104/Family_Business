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
        private readonly FamiContext _ctx = new();

        public SaleInvoiceView()
        {
            InitializeComponent();
            LoadHeaderLookups();
        }

        private void LoadHeaderLookups()
        {
            cbCustomer.ItemsSource = _ctx.Customers.ToList();
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);
        }

        private void BtnClearInvoice_Click(object sender, RoutedEventArgs e)
        {
            cbCustomer.SelectedIndex = -1;
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);
        }

        private void BtnSaveInvoice_Click(object s, RoutedEventArgs e)
        {
            if (cbCustomer.SelectedItem is not Customer cust)
            {
                MessageBox.Show(
                    "Chọn khách hàng.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning
                );
                return;
            }

            var inv = new Invoice
            {
                CustomerId = cust.CustomerID,
                InvoiceDate = dpInvoiceDate.SelectedDate.Value,
                DueDate = dpDueDate.SelectedDate,
                CreatedBy = 1 // UserID hiện tại
            };
            _ctx.Invoices.Add(inv);
            _ctx.SaveChanges();

            MessageBox.Show(
                $"Hóa đơn #{inv.InvoiceId} đã lưu!",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information
            );

            // Reset form
            cbCustomer.SelectedIndex = -1;
            dpInvoiceDate.SelectedDate = DateTime.Today;
            dpDueDate.SelectedDate = DateTime.Today.AddDays(30);
        }

        // Thêm sự kiện BtnAddCustomer_Click nếu muốn mở form thêm khách hàng mới
        private void BtnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Hiển thị form thêm khách hàng mới
            // Sau khi thêm, reload lại danh sách khách hàng:
            cbCustomer.ItemsSource = _ctx.Customers.ToList();
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
