using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Family_Business.Models;
using Microsoft.EntityFrameworkCore;

namespace Family_Business.Views
{
    public partial class PurchaseTransactionManagementView : UserControl
    {
        // DTO for grid binding
        private record DisplayTx(int TxId, DateTime TxDate, string ProductName,
                                 string UnitName, int Quantity,
                                 string SupplierName, string? Note);

        private DisplayTx? _editing;
        private List<DisplayTx> _allDisplay = new();

        public PurchaseTransactionManagementView()
        {
            InitializeComponent();
            // default date to today
            dpTxDate.SelectedDate = DateTime.Now;
            LoadLookups();
            LoadTransactions();
        }

        void LoadLookups()
        {
            using var ctx = new FamiContext();
            cbProduct.ItemsSource = ctx.Products.Include(p => p.BaseUnit).ToList();
            cbUnit.ItemsSource = ctx.Units.ToList();
            cbSupplier.ItemsSource = ctx.Suppliers.ToList();
            ClearForm();
        }

        void LoadTransactions()
        {
            using var ctx = new FamiContext();
            var txs = ctx.InventoryTransactions
                        .Where(t => t.TxType == "Purchase")
                        .Include(t => t.Product)
                        .Include(t => t.Unit)
                        .ToList();

            var supDict = ctx.Suppliers
                             .ToDictionary(s => s.SupplierId, s => s.Name);

            _allDisplay = txs.Select(t => new DisplayTx(
                t.TxId,
                t.TxDate,
                t.Product.Name,
                t.Unit.UnitName,
                (int)t.Quantity,
                t.PartyId.HasValue && supDict.TryGetValue(t.PartyId.Value, out var n) ? n : "",
                t.Note
            )).ToList();

            dgTransactions.ItemsSource = _allDisplay;
        }

        private void BtnNew_Click(object s, RoutedEventArgs e)
        {
            _editing = null;
            ClearForm();
            dgTransactions.SelectedItem = null;
        }

        private void DgTransactions_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (dgTransactions.SelectedItem is DisplayTx dto)
            {
                _editing = dto;
                using var ctx = new FamiContext();
                var tx = ctx.InventoryTransactions.Find(dto.TxId);
                if (tx == null) return;

                dpTxDate.SelectedDate = tx.TxDate;
                cbProduct.SelectedItem = ctx.Products.Find(tx.ProductId);
                cbUnit.SelectedItem = ctx.Units.Find(tx.UnitId);
                tbQuantity.Text = ((int)tx.Quantity).ToString();
                cbSupplier.SelectedItem = ctx.Suppliers.Find(tx.PartyId);
                tbNote.Text = tx.Note;
            }
        }

        private void BtnSave_Click(object s, RoutedEventArgs e)
        {
            if (cbProduct.SelectedItem is not Product prod ||
                cbUnit.SelectedItem is not Unit unit ||
                !int.TryParse(tbQuantity.Text, out var qty) ||
                cbSupplier.SelectedItem is not Supplier sup ||
                dpTxDate.SelectedDate is not DateTime date)
            {
                MessageBox.Show("Vui lòng điền đầy đủ và đúng định dạng.",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var ctx = new FamiContext();
            InventoryTransaction txEnt;
            if (_editing != null)
            {
                txEnt = ctx.InventoryTransactions.Find(_editing.TxId)!;
            }
            else
            {
                txEnt = new InventoryTransaction { TxType = "Purchase" };
                ctx.InventoryTransactions.Add(txEnt);
            }

            // preserve time-of-day
            var time = DateTime.Now.TimeOfDay;
            txEnt.TxDate = date.Date + time;
            txEnt.ProductId = prod.ProductId;
            txEnt.UnitId = unit.UnitId;
            txEnt.Quantity = qty;
            txEnt.PartyType = "Supplier";
            txEnt.PartyId = sup.SupplierId;
            txEnt.Note = string.IsNullOrWhiteSpace(tbNote.Text)
                              ? null
                              : tbNote.Text.Trim();

            ctx.SaveChanges();

            MessageBox.Show("Nhập hàng đã lưu!", "Thông báo",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            LoadTransactions();
            BtnNew_Click(s, e);
        }
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTransactions.SelectedItem == null)
            {
                MessageBox.Show("Chọn bản ghi để sửa.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // Gọi lại SelectionChanged để load dữ liệu lên form
            DgTransactions_SelectionChanged(dgTransactions, null);
        }

        private void BtnDelete_Click(object s, RoutedEventArgs e)
        {
            if (_editing == null) return;
            if (MessageBox.Show("Xác nhận xóa bản ghi này?", "Cảnh báo",
                                MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            using var ctx = new FamiContext();
            var ent = ctx.InventoryTransactions.Find(_editing.TxId);
            if (ent != null)
            {
                ctx.InventoryTransactions.Remove(ent);
                ctx.SaveChanges();
            }

            LoadTransactions();
            BtnNew_Click(s, e);
        }

        private void BtnReload_Click(object s, RoutedEventArgs e)
            => LoadTransactions();

        private void BtnClear_Click(object s, RoutedEventArgs e)
            => BtnNew_Click(s, e);

        void ClearForm()
        {
            dpTxDate.SelectedDate = DateTime.Now;
            cbProduct.SelectedIndex = -1;
            cbUnit.SelectedIndex = -1;
            tbQuantity.Clear();
            cbSupplier.SelectedIndex = -1;
            tbNote.Clear();
        }
    }
}
