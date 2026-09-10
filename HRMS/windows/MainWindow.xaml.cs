using DevExpress.Xpf.Core;
using HRMS.httpServer;
using HRMS.utils;
using HRMS.windows;
using HRMS.holders;
using HRMS.services;
using System;
using System.Windows;
using DevExpress.Xpf.Grid;
using Microsoft.Win32;

namespace HRMS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            ApplyPermissions();
        }

        private void showPayroll_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var employee = GetSelectedEmployee();
            if (employee == null)
                return;

            var payrollWindow = new PayrollWindow(employee);
            payrollWindow.Owner = this;
            payrollWindow.Show();
        }

        private void vietnamese_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(DictUtil.applyLanguage("vi-VN"));
            ApplyPermissions();
        }

        private void english_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(DictUtil.applyLanguage("en-US"));
            ApplyPermissions();
        }

        private void image_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var employee = GetSelectedEmployee();
            if (employee == null)
                return;

            var imageWindow = new ImageWindow(employee);
            imageWindow.Owner = this;
            imageWindow.Show();
        }

        private void exportPayslip_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var payroll = payrollGridControl.SelectedItem as Payroll;
            if (payroll != null)
            {
                PayslipWindow payslipWindow = new PayslipWindow(payroll);
                payslipWindow.Owner = this;
                payslipWindow.Show();

            }
        }

        private void timekeeping_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel == null || viewModel.HrmsContext == null)
                return;

            var attendanceWindow = new AttendanceHubWindow(viewModel.HrmsContext, RefreshAttendance);
            attendanceWindow.Owner = this;
            attendanceWindow.Show();
        }

        private void qrAttendance_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (HttpServer.IsRunning)
            {
                ThemedMessageBox.Show(
                    title: "Dịch vụ chấm công đang chạy",
                    text: "Chỉ có thể mở một phiên chấm công QR trên máy này.",
                    messageBoxButtons: MessageBoxButton.OK,
                    icon: MessageBoxImage.Information);
                return;
            }

            var viewModel = DataContext as MainViewModel;
            if (viewModel == null || viewModel.HrmsContext == null)
                return;

            var tokenWindow = new TokenWindow(viewModel.HrmsContext, callback);
            tokenWindow.Owner = this;
            tokenWindow.Show();
        }


        private void callback(int t)
        {
            RefreshAttendance();
        }

        private void RefreshAttendance()
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
                viewModel.ReloadTimekeepings();
        }

        private void ApplyPermissions()
        {
            var canManageEmployees = AuthorizationService.Can(Permission.ManageEmployees);
            var employeeView = employeeGridControl.View as TableView;
            if (employeeView != null)
            {
                employeeView.AllowEditing = canManageEmployees;
                employeeView.NewItemRowPosition = canManageEmployees ? NewItemRowPosition.Top : NewItemRowPosition.None;
            }
            if (!canManageEmployees)
                employeeGridControl.InputBindings.Clear();

            dateOfBirthColumn.Visible = canManageEmployees;
            genderColumn.Visible = canManageEmployees;
            ageColumn.Visible = canManageEmployees;
            phoneColumn.Visible = canManageEmployees;
            emailColumn.Visible = canManageEmployees;
            addressColumn.Visible = canManageEmployees;
            salaryColumn.Visible = canManageEmployees || AuthorizationService.Can(Permission.ViewPayroll);

            image.IsEnabled = canManageEmployees;
            qr.IsEnabled = canManageEmployees;
            timekeeping.IsEnabled = AuthorizationService.Can(Permission.RecordAttendance) || canManageEmployees;
            qrAttendance.IsEnabled = AuthorizationService.Can(Permission.RecordAttendance);
            exportAttendance.IsEnabled = AuthorizationService.Can(Permission.ViewAttendance);
            manageRoles.IsEnabled = AuthorizationService.Can(Permission.ManageRoles);
            viewAuditLog.IsEnabled = AuthorizationService.Can(Permission.ManageRoles);
            caculatePayroll.IsEnabled = AuthorizationService.Can(Permission.CalculatePayroll);
            showPayroll.IsEnabled = AuthorizationService.Can(Permission.ViewPayroll);
            exportPayslip.IsEnabled = AuthorizationService.Can(Permission.ViewPayroll);
            payrollsTab.Visibility = AuthorizationService.Can(Permission.ViewPayroll) ? Visibility.Visible : Visibility.Collapsed;
            timekeepingsTab.Visibility = AuthorizationService.Can(Permission.ViewAttendance) ? Visibility.Visible : Visibility.Collapsed;
            roleStatus.Content = String.Format(FindResource("roleStatus").ToString(), AuthHolder.Username, AuthHolder.Role);
        }

        private void manageRoles_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var window = new RoleManagementWindow { Owner = this };
            window.ShowDialog();
        }

        private void viewAuditLog_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var window = new AuditLogWindow { Owner = this };
            window.ShowDialog();
        }

        private void logout_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            AuditService.Log("LOGOUT", "Session", AuthHolder.Username, "Role=" + AuthHolder.Role);
            TokenHolder.Clear();
            AuthHolder.Clear();

            var loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            Close();
        }

        private void exportAttendance_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel == null)
                return;
            var dialog = new SaveFileDialog
            {
                Filter = "CSV UTF-8 (*.csv)|*.csv",
                FileName = "attendance-" + DateTime.Today.ToString("yyyy-MM-dd") + ".csv"
            };
            if (dialog.ShowDialog(this) != true)
                return;
            try
            {
                AttendanceReportService.ExportCsv(dialog.FileName, viewModel.Timekeepings, viewModel.Employees);
                AuditService.Log("ATTENDANCE_EXPORT", "Report", null,
                    "Rows=" + viewModel.Timekeepings.Count);
                ThemedMessageBox.Show("Xuất báo cáo", "Đã xuất báo cáo chấm công.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Xuất báo cáo", "Không thể xuất báo cáo: " + ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void qr_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            var employee = GetSelectedEmployee();
            if (employee == null)
                return;

            var qrWindow = new QRWindow(employee.id);
            qrWindow.Owner = this;
            qrWindow.Show();
        }

        private Employee GetSelectedEmployee()
        {
            var employee = employeeGridControl.SelectedItem as Employee;
            if (employee != null)
                return employee;

            ThemedMessageBox.Show(
                title: "Chưa chọn nhân viên",
                text: "Vui lòng chọn một nhân viên trước khi thực hiện chức năng này.",
                messageBoxButtons: MessageBoxButton.OK,
                icon: MessageBoxImage.Information);
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            if (viewModel != null)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
