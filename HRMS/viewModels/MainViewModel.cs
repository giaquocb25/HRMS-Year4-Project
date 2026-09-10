using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Core;
using HRMS.utils;
using HRMS.services;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Data.Entity.Infrastructure;

namespace HRMS
{
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private HUMAN_MANAGEMENTEntities hrmsContext;

        public MainViewModel()
        {
            Employees = new ObservableCollection<Employee>();
            Payrolls = new ObservableCollection<Payroll>();
            Timekeepings = new ObservableCollection<Timekeeping>();

            if (!IsInDesignMode)
                InitializeContext();
        }

        public HUMAN_MANAGEMENTEntities HrmsContext
        {
            get { return hrmsContext; }
        }

        public ObservableCollection<Employee> Employees
        {
            get { return GetValue<ObservableCollection<Employee>>(); }
            set { SetValue(value); }
        }

        public ObservableCollection<Payroll> Payrolls
        {
            get { return GetValue<ObservableCollection<Payroll>>(); }
            set { SetValue(value); }
        }

        public ObservableCollection<Timekeeping> Timekeepings
        {
            get { return GetValue<ObservableCollection<Timekeeping>>(); }
            set { SetValue(value); }
        }

        private void InitializeContext()
        {
            if (hrmsContext != null)
                hrmsContext.Dispose();

            hrmsContext = new HUMAN_MANAGEMENTEntities();
            hrmsContext.Employees.Load();
            Employees = hrmsContext.Employees.Local;
            ReloadPayrolls();
            ReloadTimekeepings();
        }

        internal void ReloadTimekeepings()
        {
            if (!AuthorizationService.Can(Permission.ViewAttendance))
            {
                Timekeepings.Clear();
                return;
            }
            Timekeepings = new ObservableCollection<Timekeeping>(
                hrmsContext.Timekeepings.AsNoTracking()
                    .OrderByDescending(item => item.workDate)
                    .ThenByDescending(item => item.arriveTime)
                    .ToList());
        }

        private void ReloadPayrolls()
        {
            if (!AuthorizationService.Can(Permission.ViewPayroll))
            {
                Payrolls.Clear();
                return;
            }
            Payrolls = new ObservableCollection<Payroll>(
                hrmsContext.Payrolls.AsNoTracking()
                    .OrderByDescending(item => item.createdTime)
                    .ToList());
        }

        [Command]
        public void ValidateEmployee(RowValidationArgs args)
        {
            if (!AuthorizationService.Can(Permission.ManageEmployees))
            {
                args.Result = new ValidationErrorInfo(
                    "Tài khoản không có quyền sửa hồ sơ nhân viên.", ValidationErrorType.Critical);
                return;
            }
            var item = args.Item as Employee;
            int age;

            if (item == null || String.IsNullOrWhiteSpace(item.name))
            {
                args.Result = new ValidationErrorInfo(
                    "Tên nhân viên không được để trống.", ValidationErrorType.Critical);
                return;
            }

            item.name = item.name.Trim();
            item.employeeCode = String.IsNullOrWhiteSpace(item.employeeCode) ? null : item.employeeCode.Trim().ToUpperInvariant();
            item.department = String.IsNullOrWhiteSpace(item.department) ? null : item.department.Trim();
            item.gender = String.IsNullOrWhiteSpace(item.gender) ? null : item.gender.Trim();
            item.phone = String.IsNullOrWhiteSpace(item.phone) ? null : item.phone.Trim();
            item.email = String.IsNullOrWhiteSpace(item.email) ? null : item.email.Trim().ToLowerInvariant();
            item.employmentStatus = String.IsNullOrWhiteSpace(item.employmentStatus) ? null : item.employmentStatus.Trim();
            if (item.name.Length > 150
                || (item.employeeCode != null && item.employeeCode.Length > 30)
                || (item.department != null && item.department.Length > 100)
                || (item.gender != null && item.gender.Length > 20)
                || (item.phone != null && item.phone.Length > 20)
                || (item.email != null && item.email.Length > 150)
                || (!String.IsNullOrWhiteSpace(item.address) && item.address.Trim().Length > 255)
                || (!String.IsNullOrWhiteSpace(item.jobTitle) && item.jobTitle.Trim().Length > 100))
            {
                args.Result = new ValidationErrorInfo(
                    "Một hoặc nhiều trường hồ sơ vượt quá độ dài cho phép.", ValidationErrorType.Critical);
                return;
            }
            item.address = String.IsNullOrWhiteSpace(item.address) ? null : item.address.Trim();
            item.jobTitle = String.IsNullOrWhiteSpace(item.jobTitle) ? null : item.jobTitle.Trim();
            if (args.IsNewItem && item.employmentStatus == null)
                item.employmentStatus = "Active";

            var allowedStatuses = new[] { "Active", "Inactive", "OnLeave" };
            if (item.employmentStatus != null
                && !allowedStatuses.Any(status => String.Equals(status, item.employmentStatus, StringComparison.OrdinalIgnoreCase)))
            {
                args.Result = new ValidationErrorInfo(
                    "Trạng thái phải là Active, Inactive hoặc OnLeave.", ValidationErrorType.Critical);
                return;
            }
            if (item.employmentStatus != null)
                item.employmentStatus = allowedStatuses.First(status => String.Equals(status, item.employmentStatus, StringComparison.OrdinalIgnoreCase));

            if (item.dateOfBirth.HasValue
                && (item.dateOfBirth.Value.Date > DateTime.Today || item.dateOfBirth.Value.Year < 1900))
            {
                args.Result = new ValidationErrorInfo(
                    "Ngày sinh không hợp lệ.", ValidationErrorType.Critical);
                return;
            }

            if (item.dateOfBirth.HasValue)
            {
                var birthday = item.dateOfBirth.Value.Date;
                var calculatedAge = DateTime.Today.Year - birthday.Year;
                if (birthday > DateTime.Today.AddYears(-calculatedAge))
                    calculatedAge--;
                item.age = calculatedAge.ToString();
            }

            if (!String.IsNullOrWhiteSpace(item.age)
                && (!Int32.TryParse(item.age, out age) || age < 16 || age > 100))
            {
                args.Result = new ValidationErrorInfo(
                    "Tuổi phải là số từ 16 đến 100.", ValidationErrorType.Critical);
                return;
            }

            if (args.IsNewItem && String.IsNullOrWhiteSpace(item.employeeCode))
            {
                args.Result = new ValidationErrorInfo(
                    "Mã nhân viên không được để trống.", ValidationErrorType.Critical);
                return;
            }

            if (!String.IsNullOrWhiteSpace(item.employeeCode)
                && hrmsContext.Employees.Any(employee => employee.id != item.id && employee.employeeCode == item.employeeCode))
            {
                args.Result = new ValidationErrorInfo(
                    "Mã nhân viên đã tồn tại.", ValidationErrorType.Critical);
                return;
            }

            if (!String.IsNullOrWhiteSpace(item.phone)
                && !Regex.IsMatch(item.phone.Trim(), @"^\+?[0-9][0-9 .-]{7,18}$"))
            {
                args.Result = new ValidationErrorInfo(
                    "Số điện thoại không hợp lệ.", ValidationErrorType.Critical);
                return;
            }

            if (!String.IsNullOrWhiteSpace(item.email))
            {
                try
                {
                    var address = new MailAddress(item.email.Trim());
                    if (!String.Equals(address.Address, item.email.Trim(), StringComparison.OrdinalIgnoreCase))
                        throw new FormatException();
                }
                catch (FormatException)
                {
                    args.Result = new ValidationErrorInfo(
                        "Địa chỉ email không hợp lệ.", ValidationErrorType.Critical);
                    return;
                }

                if (hrmsContext.Employees.Any(employee => employee.id != item.id && employee.email == item.email))
                {
                    args.Result = new ValidationErrorInfo(
                        "Địa chỉ email đã được sử dụng.", ValidationErrorType.Critical);
                    return;
                }
            }

            if (item.salary < 0)
            {
                args.Result = new ValidationErrorInfo(
                    "Lương cơ bản không được nhỏ hơn 0.", ValidationErrorType.Critical);
                return;
            }

            var now = DateTime.Now;
            if (args.IsNewItem)
            {
                item.createdTime = now;
                hrmsContext.Employees.Add(item);
            }

            item.updatedTime = now;
            try
            {
                hrmsContext.SaveChanges();
                AuditService.Log(args.IsNewItem ? "EMPLOYEE_CREATE" : "EMPLOYEE_UPDATE",
                    "Employee", item.id.ToString(), item.employeeCode + " - " + item.name);
            }
            catch (DbUpdateException)
            {
                if (args.IsNewItem)
                    hrmsContext.Entry(item).State = EntityState.Detached;
                else
                    hrmsContext.Entry(item).Reload();
                args.Result = new ValidationErrorInfo(
                    "Không thể lưu hồ sơ. Hãy kiểm tra mã nhân viên, email và dữ liệu bắt buộc.",
                    ValidationErrorType.Critical);
            }
        }

        [Command]
        public void ValidateEmployeeDeletion(ValidateRowDeletionArgs args)
        {
            if (!AuthorizationService.Can(Permission.ManageEmployees))
            {
                args.Result = new ValidationErrorInfo(
                    "Tài khoản không có quyền xóa hồ sơ nhân viên.", ValidationErrorType.Critical);
                return;
            }
            if (args.Items == null || args.Items.Length == 0)
                return;

            var employees = args.Items.OfType<Employee>().Distinct().ToList();
            if (employees.Count == 0)
                return;

            var confirmation = ThemedMessageBox.Show(
                title: "Xác nhận xóa nhân viên",
                text: employees.Count == 1
                    ? "Xóa nhân viên “" + employees[0].name + "” và toàn bộ dữ liệu công/lương liên quan?"
                    : "Xóa " + employees.Count + " nhân viên đã chọn và toàn bộ dữ liệu công/lương liên quan?",
                messageBoxButtons: MessageBoxButton.YesNo,
                defaultButton: MessageBoxResult.No,
                icon: MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
            {
                args.DisplayErrorMode = DisplayDeleteOperationError.Disabled;
                args.Result = new ValidationErrorInfo("Đã hủy thao tác xóa.", ValidationErrorType.Information);
                return;
            }

            try
            {
                using (var transaction = hrmsContext.Database.BeginTransaction())
                {
                    // Các khóa ngoại trong schema dùng ON DELETE CASCADE, nên HR chỉ cần
                    // quyền DELETE trên Employees thay vì quyền xóa trực tiếp dữ liệu công/lương.
                    hrmsContext.Employees.RemoveRange(employees);
                    hrmsContext.SaveChanges();
                    transaction.Commit();
                }

                foreach (var item in employees)
                    AuditService.Log("EMPLOYEE_DELETE", "Employee", item.id.ToString(),
                        item.employeeCode + " - " + item.name);
                ReloadPayrolls();
                ReloadTimekeepings();
            }
            catch (DbUpdateException)
            {
                args.Result = new ValidationErrorInfo(
                    "Không thể xóa hồ sơ. Dữ liệu đã được giữ nguyên.", ValidationErrorType.Critical);
                InitializeContext();
            }
        }

        [Command]
        public void EmployeeRefresh(DataSourceRefreshArgs args)
        {
            InitializeContext();
        }

        [Command]
        public void CalculatePayrolls()
        {
            if (!AuthorizationService.Can(Permission.CalculatePayroll))
                return;
            var dict = DictUtil.getCurrent();
            var result = ThemedMessageBox.Show(
                title: dict["caculatePayrollTitle"].ToString(),
                text: dict["caculatePayrollText"].ToString(),
                messageBoxButtons: MessageBoxButton.YesNo,
                defaultButton: MessageBoxResult.No,
                icon: MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
                return;

            var monthStart = TimeUtil.GetCurrentMonthStart();
            var nextMonth = monthStart.AddMonths(1);
            var month = TimeUtil.GetCurrentMonth();
            var employeesById = Employees.ToDictionary(item => item.id);
            var workdaysByEmployee = hrmsContext.Timekeepings.AsNoTracking()
                .Where(item => item.workDate >= monthStart && item.workDate < nextMonth)
                .ToList()
                .Where(TimeUtil.IsFullWorkday)
                .GroupBy(item => item.employeeId)
                .Select(group => new
                {
                    EmployeeId = group.Key,
                    Workdays = group.Select(item => item.workDate.Date).Distinct().Count()
                })
                .Where(item => employeesById.ContainsKey(item.EmployeeId))
                .ToDictionary(item => item.EmployeeId, item => item.Workdays);

            var now = DateTime.Now;
            var payrolls = Employees
                .Where(employee => !String.Equals(employee.employmentStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
                .Select(employee =>
            {
                int workdays;
                workdaysByEmployee.TryGetValue(employee.id, out workdays);
                var payableDays = Math.Min(workdays, TimeUtil.StandardWorkingDays);
                var amount = Decimal.Round(
                    (decimal)employee.salary * payableDays / TimeUtil.StandardWorkingDays,
                    0,
                    MidpointRounding.AwayFromZero);

                return new Payroll
                {
                    employeeId = employee.id,
                    month = month,
                    workdays = workdays,
                    amount = Decimal.ToInt32(amount),
                    createdTime = now,
                    updatedTime = now
                };
            }).ToList();

            using (var transaction = hrmsContext.Database.BeginTransaction())
            {
                var oldPayrolls = hrmsContext.Payrolls.Where(item => item.month == month).ToList();
                hrmsContext.Payrolls.RemoveRange(oldPayrolls);
                hrmsContext.Payrolls.AddRange(payrolls);
                hrmsContext.SaveChanges();
                transaction.Commit();
            }

            AuditService.Log("PAYROLL_CALCULATE", "Payroll", month,
                "Employees=" + payrolls.Count);

            ReloadPayrolls();
            ThemedMessageBox.Show(
                title: dict["payrollCompletedTitle"].ToString(),
                text: String.Format(dict["payrollCompletedText"].ToString(), payrolls.Count),
                messageBoxButtons: MessageBoxButton.OK,
                icon: MessageBoxImage.Information);
        }

        public void Dispose()
        {
            if (hrmsContext != null)
            {
                hrmsContext.Dispose();
                hrmsContext = null;
            }
        }
    }


}
