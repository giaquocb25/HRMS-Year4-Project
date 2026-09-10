using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.BarCode;
using HRMS.holders;
using HRMS.utils;
using HRMS.httpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace HRMS.windows
{
    /// <summary>
    /// Interaction logic for TokenWindow.xaml
    /// </summary>
    public partial class TokenWindow : ThemedWindow
    {
        private HUMAN_MANAGEMENTEntities hrms;
        Action<int> callback;
        public TokenWindow(HUMAN_MANAGEMENTEntities hrms, Action<int> callback)
        {
            InitializeComponent();
            generateQRCode();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            this.hrms = hrms;
            this.callback = callback;
        }

        private void generateQRCode()
        {
            string token = TokenHolder.Generate(TimeSpan.FromMinutes(30));
            var connectionInfo = token + "," + NetworkUtil.GetLocalIPv4() + "," + HttpServer.GetPort();
            qr.EditValue = connectionInfo;
            connectionInfoTextBox.Text = connectionInfo;
        }

        private void confirm_Click(object sender, RoutedEventArgs e)
        {
            if (HttpServer.IsRunning)
            {
                ThemedMessageBox.Show(
                    title: "Dịch vụ chấm công đang chạy",
                    text: "Một phiên chấm công QR khác đã được mở trên máy này.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Information);
                Close();
                return;
            }

            try
            {
                var timekeepingWindow = new TimekeepingWindow(hrms, callback);
                timekeepingWindow.Owner = Owner;
                timekeepingWindow.Show();
                Close();
            }
            catch (Exception)
            {
                TokenHolder.Clear();
                ThemedMessageBox.Show(
                    title: "Không thể mở dịch vụ chấm công",
                    text: "Cổng 8080 chưa thể sử dụng. Hãy chạy ứng dụng với quyền phù hợp hoặc kiểm tra ứng dụng khác đang dùng cổng này.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!HttpServer.IsRunning)
                TokenHolder.Clear();
            base.OnClosed(e);
        }
    }
}
