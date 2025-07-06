using System;
using System.Linq;
using System.Windows;
using Family_Business.Helpers;
using Family_Business.Models;

namespace Family_Business.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var userName = txtUser.Text.Trim();
            var pwd = txtPass.Password;

            // 1. Kiểm trống
            if (string.IsNullOrWhiteSpace(userName))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập.");
                txtUser.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(pwd))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu.");
                txtPass.Focus();
                return;
            }

            // 2. Lấy user từ DB
            using var ctx = new FamiContext();
            var user = ctx.Users.SingleOrDefault(u => u.Username == userName);

            // 3. Kiểm tồn tại user trước khi làm gì khác
            if (user == null)
            {
                MessageBox.Show("Tên đăng nhập không tồn tại.", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                txtUser.Focus();
                return;      // ← cực kỳ quan trọng: buộc thoát luôn không chạy phần dưới
            }

            // 4. Kiểm độ dài mật khẩu (hoặc bạn có thể bỏ nếu không cần)
            if (pwd.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự.", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPass.Focus();
                return;
            }

            // 5. Hash và so sánh mật khẩu
            var saltBytes = Convert.FromBase64String(user.Salt);
            var hashed = PasswordHelper.Hash(pwd, saltBytes);
            if (hashed != user.PasswordHash)
            {
                MessageBox.Show("Mật khẩu không đúng.", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                txtPass.Clear();
                txtPass.Focus();
                return;
            }

            // 6. Thành công
            Session.Set(user);
            new MainWindow().Show();
            Close();
        }
    }
}
