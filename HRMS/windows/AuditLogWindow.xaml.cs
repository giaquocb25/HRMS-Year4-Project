using DevExpress.Xpf.Core;
using HRMS.services;
using HRMS.utils;
using System;
using System.Windows;

namespace HRMS.windows
{
    public partial class AuditLogWindow : ThemedWindow
    {
        public AuditLogWindow()
        {
            InitializeComponent();
            Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            Reload();
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void Reload()
        {
            try
            {
                auditGrid.ItemsSource = AuditService.GetRecent(1000);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Nhật ký", "Không thể tải nhật ký: " + ex.Message,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
