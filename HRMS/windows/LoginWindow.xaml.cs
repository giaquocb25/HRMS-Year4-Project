using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HRMS.holders;
using System.Data.Entity;
using DevExpress.Xpf.Core;
using System.Threading;
using HRMS.utils;
using HRMS.services;

namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.applyLanguage("vi-VN"));
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            var username = txtUser.Text == null ? String.Empty : txtUser.Text.Trim();
            if (String.IsNullOrWhiteSpace(username) || String.IsNullOrEmpty(txtPass.Password))
            {
                ShowLoginError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return;
            }

            AuthHolder.SetCredentials(username, txtPass.Password);
            btnLogin.IsEnabled = false;
            ld.IsSplashScreenShown = true;

            try
            {
                var canConnect = await Task.Run(() =>
                {
                    using (var hrms = new HUMAN_MANAGEMENTEntities())
                        return hrms.Database.Exists();
                });

                if (!canConnect)
                    throw new InvalidOperationException("Database is unavailable.");

                var role = await Task.Run(() => AuthorizationService.LoadRole(username, txtPass.Password));
                AuthHolder.SetRole(role);
                await Task.Run(() => DatabaseSchemaService.ValidateCurrentSchema());
                AuditService.Log("LOGIN", "Session", username, "Role=" + role);

                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow = mainWindow;
                Close();
            }
            catch (DatabaseMigrationException ex)
            {
                AuthHolder.Clear();
                ShowLoginError(ex.Message);
            }
            catch (AccountDisabledException ex)
            {
                AuthHolder.Clear();
                ShowLoginError(ex.Message);
            }
            catch (Exception)
            {
                AuthHolder.Clear();
                ShowLoginError("Không thể kết nối cơ sở dữ liệu. Hãy kiểm tra tài khoản và cấu hình máy chủ.");
            }
            finally
            {
                ld.IsSplashScreenShown = false;
                btnLogin.IsEnabled = true;
            }
        }

        private static void ShowLoginError(string message)
        {
            ThemedMessageBox.Show(
                title: "Đăng nhập không thành công",
                text: message,
                messageBoxButtons: MessageBoxButton.OK,
                icon: MessageBoxImage.Exclamation);
        }
    }
}
