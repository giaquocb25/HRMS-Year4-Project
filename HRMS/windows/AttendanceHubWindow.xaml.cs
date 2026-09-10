using DevExpress.Xpf.Core;
using HRMS.models;
using HRMS.services;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HRMS.utils;

namespace HRMS.windows
{
    public partial class AttendanceHubWindow : ThemedWindow
    {
        private readonly HUMAN_MANAGEMENTEntities context;
        private readonly AttendanceService attendanceService = new AttendanceService();
        private readonly Action callback;

        public AttendanceHubWindow(HUMAN_MANAGEMENTEntities context, Action callback)
        {
            InitializeComponent();
            Resources.MergedDictionaries.Add(DictUtil.getCurrent());
            if (context == null)
                throw new ArgumentNullException("context");
            this.context = context;
            this.callback = callback;
            var employees = context.Employees.AsNoTracking().OrderBy(item => item.name).ToList();
            employeeComboBox.ItemsSource = employees
                .Where(item => !String.Equals(item.employmentStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                .ToList();
            correctionEmployeeComboBox.ItemsSource = employees;
            correctionDatePicker.SelectedDate = DateTime.Today;
            machineProviderComboBox.ItemsSource = new[] { "MySQL", "SQL Server" };
            machineProviderComboBox.SelectionChanged += delegate
            {
                var provider = machineProviderComboBox.SelectedItem == null
                    ? String.Empty
                    : machineProviderComboBox.SelectedItem.ToString();
                if (provider == "SQL Server" && machinePortTextBox.Text == "3306")
                    machinePortTextBox.Text = "1433";
                else if (provider == "MySQL" && machinePortTextBox.Text == "1433")
                    machinePortTextBox.Text = "3306";
            };
            machineProviderComboBox.SelectedIndex = 0;
            machineSincePicker.SelectedDate = DateTime.Today.AddDays(-1);
            var canRecord = AuthorizationService.Can(Permission.RecordAttendance);
            var canManageCards = AuthorizationService.Can(Permission.ManageEmployees);
            var canImport = AuthorizationService.Can(Permission.ImportAttendance);
            var canCorrect = AuthorizationService.Can(Permission.CorrectAttendance);
            identifierTextBox.IsEnabled = canRecord;
            recordNowButton.IsEnabled = canRecord;
            employeeComboBox.IsEnabled = canManageCards;
            cardUidTextBox.IsEnabled = canManageCards;
            bindCardButton.IsEnabled = canManageCards;
            importFileButton.IsEnabled = canImport;
            syncMachineButton.IsEnabled = canImport;
            correctionTab.Visibility = canCorrect ? Visibility.Visible : Visibility.Collapsed;
            Loaded += delegate { identifierTextBox.Focus(); };
            Loaded += delegate { RefreshAudit(); };
        }

        private void identifierTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RecordIdentifier();
                e.Handled = true;
            }
        }

        private void recordNow_Click(object sender, RoutedEventArgs e)
        {
            RecordIdentifier();
        }

        private void RecordIdentifier()
        {
            var identifier = (identifierTextBox.Text ?? String.Empty).Trim();
            int employeeId;
            var isInternalId = Int32.TryParse(identifier, out employeeId);
            var result = attendanceService.Record(new AttendanceInput
            {
                EmployeeId = isInternalId ? (int?)employeeId : null,
                EmployeeCode = identifier,
                CardUid = identifier,
                EventTime = DateTime.Now,
                Source = "terminal",
                DeviceName = Environment.MachineName
            });
            ShowResult(result);
            identifierTextBox.Clear();
            identifierTextBox.Focus();
        }

        private void cardUidTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BindCard();
                e.Handled = true;
            }
        }

        private void bindCard_Click(object sender, RoutedEventArgs e)
        {
            BindCard();
        }

        private void BindCard()
        {
            var employee = employeeComboBox.SelectedItem as Employee;
            if (employee == null)
            {
                statusTextBlock.Text = "Hãy chọn nhân viên trước khi gán thẻ.";
                return;
            }
            ShowResult(attendanceService.BindCard(employee.id, cardUidTextBox.Text));
            cardUidTextBox.Clear();
        }

        private void chooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Dữ liệu chấm công (*.xlsx;*.csv;*.tsv;*.txt)|*.xlsx;*.csv;*.tsv;*.txt|Tất cả file (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) == true)
                filePathTextBox.Text = dialog.FileName;
        }

        private async void importFile_Click(object sender, RoutedEventArgs e)
        {
            await RunImport(() => new AttendanceFileImporter().Import(filePathTextBox.Text));
        }

        private async void syncMachine_Click(object sender, RoutedEventArgs e)
        {
            uint port;
            if (!UInt32.TryParse(machinePortTextBox.Text, out port) || port == 0 || port > 65535)
            {
                statusTextBlock.Text = "Cổng database không hợp lệ.";
                return;
            }

            var settings = new MachineDatabaseSettings
            {
                Server = machineServerTextBox.Text.Trim(),
                Provider = machineProviderComboBox.SelectedItem == null ? "MySQL" : machineProviderComboBox.SelectedItem.ToString(),
                Port = port,
                Database = machineDatabaseTextBox.Text.Trim(),
                Username = machineUsernameTextBox.Text.Trim(),
                Password = machinePasswordBox.Password,
                ViewName = machineViewTextBox.Text.Trim(),
                Since = machineSincePicker.SelectedDate ?? DateTime.Today.AddDays(-1)
            };
            await RunImport(() => new MachineDatabaseImporter().Import(settings));
        }

        private async Task RunImport(Func<AttendanceImportSummary> action)
        {
            IsEnabled = false;
            statusTextBlock.Text = "Đang xử lý...";
            try
            {
                var summary = await Task.Run(action);
                statusTextBlock.Text = String.Format(
                    "Tổng: {0} | Đã nhập: {1} | Trùng: {2} | Lỗi: {3}{4}{5}",
                    summary.Total, summary.Imported, summary.Duplicates, summary.Failed,
                    Environment.NewLine, summary.Details);
                if (callback != null)
                    callback();
                RefreshAudit();
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Không thể nhập dữ liệu: " + ex.Message;
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void ShowResult(AttendanceResult result)
        {
            statusTextBlock.Text = result == null ? "Không có kết quả." : result.Message;
            if (result != null && result.Success && callback != null)
                callback();
            if (result != null && result.Success)
                RefreshAudit();
        }

        private void correctAttendance_Click(object sender, RoutedEventArgs e)
        {
            var employee = correctionEmployeeComboBox.SelectedItem as Employee;
            if (employee == null || !correctionDatePicker.SelectedDate.HasValue)
            {
                statusTextBlock.Text = "Hãy chọn nhân viên và ngày cần điều chỉnh.";
                return;
            }

            TimeSpan arriveTime;
            var leaveTime = TimeSpan.Zero;
            if (!TryParseClock(correctionArriveTextBox.Text, out arriveTime)
                || (!String.IsNullOrWhiteSpace(correctionLeaveTextBox.Text)
                    && !TryParseClock(correctionLeaveTextBox.Text, out leaveTime)))
            {
                statusTextBlock.Text = "Giờ phải có định dạng HH:mm hoặc HH:mm:ss.";
                return;
            }
            if (String.IsNullOrWhiteSpace(correctionLeaveTextBox.Text))
                leaveTime = TimeSpan.Zero;

            var result = attendanceService.CorrectDailyAttendance(
                employee.id,
                correctionDatePicker.SelectedDate.Value.Date,
                arriveTime,
                leaveTime,
                correctionReasonTextBox.Text);
            ShowResult(result);
            if (result.Success)
                correctionReasonTextBox.Clear();
        }

        private static bool TryParseClock(string value, out TimeSpan result)
        {
            return TimeSpan.TryParseExact((value ?? String.Empty).Trim(),
                new[] { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" },
                CultureInfo.InvariantCulture, out result);
        }

        private void refreshAudit_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudit();
        }

        private void RefreshAudit()
        {
            try
            {
                auditGrid.ItemsSource = attendanceService.GetRecentEvents(500);
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Không thể tải nhật ký: " + ex.Message;
            }
        }
    }
}
