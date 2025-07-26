using System.Windows;
using System.Windows.Controls;

namespace Family_Business.Views
{
    public partial class AdminMenuWindow : Window
    {
        public AdminMenuWindow()
        {
            InitializeComponent();
        }

        // Helper để show UserControl trong Window mới
        private void ShowUserControlInDialog(UserControl uc, string title = "Chi tiết")
        {
            var win = new Window
            {
                Title = title,
                Content = uc,
                Width = 950,
                Height = 650,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };
            win.ShowDialog();
        }

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new NewInvoiceView(), "Tạo & Quản lý hóa đơn bán hàng");
        }

        private void BtnNewPurchaseInvoice_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new NewPurchaseInvoiceView(), "Tạo & Quản lý hóa đơn mua hàng");
        }

        private void BtnCustomer_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new CustomerView(), "Quản lý khách hàng");
        }

        private void BtnSupplier_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new SupplierView(), "Quản lý nhà cung cấp");
        }

        private void BtnProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new ProductView(), "Quản lý sản phẩm");
        }

        private void BtnProductCategory_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new ProductCategoryView(), "Quản lý danh mục sản phẩm");
        }

        private void BtnUnit_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new UnitView(), "Quản lý đơn vị tính");
        }

        private void BtnInventory_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new InventoryTransactionListView(), "Quản lý tồn kho & nhập/xuất kho");
        }

        private void BtnTransactionHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new TransactionHistoryView(), "Lịch sử giao dịch");
        }

        private void BtnDebtOverview_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new DebtOverviewView(), "Tổng nợ khách hàng");
        }

        private void BtnSupplierDebtOverview_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new SupplierDebtOverviewView(), "Tổng nợ nhà cung cấp");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }
    }
}
