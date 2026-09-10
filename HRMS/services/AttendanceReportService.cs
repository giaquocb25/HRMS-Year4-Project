using HRMS.models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace HRMS.services
{
    internal static class AttendanceReportService
    {
        public static void ExportCsv(string path, IEnumerable<Timekeeping> records, IEnumerable<Employee> employees)
        {
            if (!AuthorizationService.Can(Permission.ViewAttendance))
                throw new UnauthorizedAccessException("Tài khoản không có quyền xuất báo cáo chấm công.");
            var employeeById = employees.ToDictionary(item => item.id);
            var builder = new StringBuilder();
            builder.AppendLine("employee_code,employee_name,work_date,arrive_time,leave_time,worked_hours,status");
            foreach (var record in records.OrderBy(item => item.workDate).ThenBy(item => item.employeeId))
            {
                Employee employee;
                employeeById.TryGetValue(record.employeeId, out employee);
                builder.AppendLine(String.Join(",", new[]
                {
                    Csv(employee == null ? String.Empty : employee.employeeCode),
                    Csv(employee == null ? "#" + record.employeeId : employee.name),
                    record.workDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    record.arriveTime.ToString(@"hh\:mm\:ss"),
                    record.leaveTime == TimeSpan.Zero ? String.Empty : record.leaveTime.ToString(@"hh\:mm\:ss"),
                    record.workedHours.ToString("0.00", CultureInfo.InvariantCulture),
                    Csv(record.attendanceStatus)
                }));
            }
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        private static string Csv(string value)
        {
            value = value ?? String.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
