using HRMS.holders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HRMS.services
{
    internal sealed class DatabaseMigrationException : Exception
    {
        public DatabaseMigrationException(string message) : base(message) { }
    }

    internal static class DatabaseSchemaService
    {
        public static void ValidateCurrentSchema()
        {
            var missing = new List<string>();
            using (var connection = new MySqlConnection(
                DBManagement.GetProviderConnectionString(AuthHolder.Username, AuthHolder.Password)))
            {
                connection.Open();
                foreach (var table in new[] { "UserRoles", "SystemAuditLogs" })
                    if (!Exists(connection,
                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name",
                        table))
                        missing.Add("table " + table);

                foreach (var column in new[] { "employeeCode", "department", "dateOfBirth", "gender", "phone", "email", "employmentStatus" })
                    if (!Exists(connection,
                        "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'Employees' AND column_name = @name",
                        column))
                        missing.Add("Employees." + column);

                if (AuthorizationService.Can(Permission.ManageEmployees))
                {
                    foreach (var index in new[] { "UX_Employees_EmployeeCode", "UX_Employees_Email" })
                        if (!Exists(connection,
                            "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'Employees' AND index_name = @name",
                            index))
                            missing.Add("index " + index);
                }

                if (AuthorizationService.Can(Permission.RecordAttendance)
                    || AuthorizationService.Can(Permission.ManageEmployees))
                {
                    foreach (var table in new[] { "EmployeeCards", "AttendanceEvents" })
                        if (!Exists(connection,
                            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @name",
                            table))
                            missing.Add("table " + table);

                    if (!Exists(connection,
                        "SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'Timekeeping' AND index_name = @name",
                        "UX_Timekeeping_Employee_Date"))
                        missing.Add("index UX_Timekeeping_Employee_Date");
                }

            }

            if (missing.Count > 0)
                throw new DatabaseMigrationException(
                    "Database chưa được nâng cấp (thiếu " + String.Join(", ", missing) + "). " +
                    "Hãy sao lưu rồi chạy Database/migrate_2026_multisource_attendance.sql.");
        }

        private static bool Exists(MySqlConnection connection, string sql, string name)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.Parameters.AddWithValue("@name", name);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }
    }
}
