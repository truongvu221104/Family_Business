using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class PurchaseTransactionManagementView : UserControl
    {
        // Đối tượng đang được sửa (null nếu thêm mới)
        private InventoryTransaction _editing;

        public PurchaseTransactionManagementView()
        {
            InitializeComponent();
            LoadLookups();
            LoadTransactions();
        }

        /// <summary>Load dữ liệu cho các ComboBox lookup</summary>
        private void LoadLookups()
        {
            using var ctx = new FamiContext();
            cbProduct.ItemsSource = ctx.Products.ToList();
            cbUnit.ItemsSource = ctx.Units.ToList();
            cbSupplier.ItemsSource = ctx.Suppliers.ToList();
            ClearForm();
        }

        /// <summary>Load danh sách giao dịch loại Purchase</summary>
        private void LoadTransactions()
        {
            using var ctx = new FamiContext();

            // 1) Lấy hết giao dịch Purchase vào memory trước
            var txList = ctx.InventoryTransactions
                            .Include(t => t.Product)
                            .Include(t => t.Unit)
                            .Where(t => t.TxType == "Purchase")
                            .ToList();    // <-- ToList() ở đây

            // 2) Lấy luôn dictionary Suppliers để lookup nhanh
            var suppliers = ctx.Suppliers
                               .ToDictionary(s => s.SupplierId, s => s.Name);

            // 3) Project ra object để binding
            var list = txList
                .Select(t => new {
                    TxId = t.TxId,
                    t.TxDate,
                    t.Product,
                    t.Unit,
                    t.Quantity,
                    PartyName = t.PartyId.HasValue && suppliers.TryGetValue(t.PartyId.Value, out var n)
                                   ? n
                                   : "",
                    t.Note
                })
                .ToList();

            dgTransactions.ItemsSource = list;
        }


        /// <summary>Nhấn Thêm mới → reset form</summary>
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            ClearForm();
        }

        /// <summary>Chọn 1 dòng trong DataGrid để sửa</summary>
        private void DgTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgTransactions.SelectedItem == null) return;
            dynamic row = dgTransactions.SelectedItem;
            int id = (int)row.TxId;

            using var ctx = new FamiContext();
            _editing = ctx.InventoryTransactions.Find(id);
            if (_editing == null) return;

            // Đưa giá trị lên form
            cbProduct.SelectedItem = ctx.Products.Find(_editing.ProductId);
            cbUnit.SelectedItem = ctx.Units.Find(_editing.UnitId);
            tbQuantity.Text = ((int)_editing.Quantity).ToString();
            cbSupplier.SelectedItem = ctx.Suppliers.Find(_editing.PartyId);
            tbNote.Text = _editing.Note;
        }

        /// <summary>Nhấn Lưu → thêm mới hoặc cập nhật</summary>
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (cbProduct.SelectedItem is not Product prod ||
                cbUnit.SelectedItem is not Unit unit ||
                !decimal.TryParse(tbQuantity.Text, out var qty) ||
                cbSupplier.SelectedItem is not Supplier sup)
            {
                MessageBox.Show(
                    "Vui lòng điền đủ và đúng định dạng.",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            using var ctx = new FamiContext();

            // Nếu đang edit, lấy lại entity từ context, ngược lại khởi tạo mới
            var tx = _editing != null
                   ? ctx.InventoryTransactions.Find(_editing.TxId)
                   : new InventoryTransaction { TxDate = DateTime.Now };

            tx.TxType = "Purchase";
            tx.ProductId = prod.ProductId;
            tx.UnitId = unit.UnitId;
            tx.Quantity = qty;                        // luôn dương với Purchase
            tx.PartyType = "Supplier";
            tx.PartyId = sup.SupplierId;
            tx.Note = string.IsNullOrWhiteSpace(tbNote.Text)
                              ? null
                              : tbNote.Text.Trim();

            if (_editing == null)
                ctx.InventoryTransactions.Add(tx);

            ctx.SaveChanges();

            MessageBox.Show(
                "Lưu thành công!",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            ClearForm();
            LoadTransactions();
        }

        /// <summary>Chuyển sang chế độ Edit: nạp dữ liệu bản ghi đang chọn lên form</summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTransactions.SelectedItem == null)
            {
                MessageBox.Show("Chọn bản ghi để sửa.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // Gọi lại SelectionChanged để nạp dữ liệu
            DgTransactions_SelectionChanged(dgTransactions, null);
        }

        /// <summary>Nhấn Xóa → xóa bản ghi đã chọn</summary>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTransactions.SelectedItem == null) return;
            dynamic row = dgTransactions.SelectedItem;
            int id = (int)row.TxID;

            if (MessageBox.Show(
                    "Xác nhận xóa bản ghi này?",
                    "Cảnh báo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                ) != MessageBoxResult.Yes)
                return;

            using var ctx = new FamiContext();
            var tx = ctx.InventoryTransactions.Find(id);
            if (tx != null)
            {
                ctx.InventoryTransactions.Remove(tx);
                ctx.SaveChanges();
            }

            ClearForm();
            LoadTransactions();
        }

        /// <summary>Nhấn Xóa form → reset form</summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            ClearForm();
        }

        /// <summary>Reset giá trị form về mặc định</summary>
        private void ClearForm()
        {
            cbProduct.SelectedIndex = -1;
            cbUnit.SelectedIndex = -1;
            cbSupplier.SelectedIndex = -1;
            tbQuantity.Clear();
            tbNote.Clear();
        }

        /// <summary>Host ProductView vào Window động rồi ShowDialog()</summary>
        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            var uc = new ProductView();
            var host = new Window
            {
                Title = "Quản lý Sản phẩm",
                Content = uc,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            host.ShowDialog();
            LoadLookups();
        }

        /// <summary>Host UnitView vào Window động rồi ShowDialog()</summary>
        private void BtnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            var uc = new UnitView();
            var host = new Window
            {
                Title = "Quản lý Đơn vị",
                Content = uc,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            host.ShowDialog();
            LoadLookups();
        }

        /// <summary>Host SupplierView vào Window động rồi ShowDialog()</summary>
        private void BtnAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var uc = new SupplierView();
            var host = new Window
            {
                Title = "Quản lý Nhà cung cấp",
                Content = uc,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            host.ShowDialog();
            LoadLookups();
        }

        /// <summary>Reload lại danh sách khi nhấn Tải lại</summary>
        private void BtnReload_Click(object sender, RoutedEventArgs e)
            => LoadTransactions();
    }
}
