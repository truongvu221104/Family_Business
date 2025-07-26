using System.Windows;
using System.Windows.Controls;

namespace Family_Business.Views
{
    public partial class StaffMenuWindow : Window
    {
        public StaffMenuWindow()
        {
            InitializeComponent();
        }

        // Helper method để hiển thị UserControl trong một Window mới
        private void ShowUserControlInDialog(UserControl uc, string title = "Chi tiết")
        {
            var win = new Window
            {
                Title = title,
                Content = uc,
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = this
            };
            win.ShowDialog();
        }

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new NewInvoiceView(), "Tạo & Quản lý hóa đơn bán hàng");
        }

        private void BtnCustomer_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new CustomerView(), "Quản lý khách hàng");
        }

        private void BtnTransactionHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowUserControlInDialog(new TransactionHistoryView(), "Lịch sử giao dịch");
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Quay lại màn đăng nhập
            var loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }
    }
}
