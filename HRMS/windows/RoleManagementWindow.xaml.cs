using DevExpress.Xpf.Core;
using HRMS.holders;
using HRMS.services;
using System;
using System.Linq;
using System.Windows;
using HRMS.utils;

namespace HRMS.windows
{
    public partial class RoleManagementWindow : ThemedWindow
    {
        public RoleManagementWindow()
        {
            InitializeComponent();
            Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            roleComboBox.ItemsSource = Enum.GetNames(typeof(UserRole));
            roleComboBox.SelectedItem = UserRole.Viewer.ToString();
            rolesGrid.SelectionChanged += delegate
            {
                var selected = rolesGrid.SelectedItem as RoleAssignment;
                if (selected == null)
                    return;
                usernameTextBox.Text = selected.Username;
                roleComboBox.SelectedItem = selected.Role;
                activeCheckBox.IsChecked = selected.IsActive;
            };
            Reload();
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AuthorizationService.SaveAssignment(new RoleAssignment
                {
                    Username = usernameTextBox.Text,
                    Role = roleComboBox.SelectedItem == null ? null : roleComboBox.SelectedItem.ToString(),
                    IsActive = activeCheckBox.IsChecked == true
                });
                statusText.Text = "Đã lưu. Quyền mới áp dụng từ lần đăng nhập tiếp theo.";
                Reload();
            }
            catch (Exception ex)
            {
                statusText.Text = "Không thể lưu: " + ex.Message;
            }
        }

        private void Reload()
        {
            try
            {
                rolesGrid.ItemsSource = AuthorizationService.GetAssignments().ToList();
            }
            catch (Exception ex)
            {
                statusText.Text = "Không thể tải danh sách: " + ex.Message;
            }
        }
    }
}
