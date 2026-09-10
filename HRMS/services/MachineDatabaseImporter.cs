using HRMS.models;
using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.Common;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace HRMS.services
{
    internal sealed class MachineDatabaseSettings
    {
        public string Server { get; set; }
        public string Provider { get; set; }
        public uint Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ViewName { get; set; }
        public DateTime Since { get; set; }
    }

    internal sealed class MachineDatabaseImporter
    {
        public AttendanceImportSummary Import(MachineDatabaseSettings settings)
        {
            var summary = new AttendanceImportSummary();
            if (!AuthorizationService.Can(Permission.ImportAttendance))
            {
                summary.Failed = 1;
                summary.Details = "Tài khoản không có quyền đồng bộ máy chấm công.";
                return summary;
            }
            if (settings == null
                || String.IsNullOrWhiteSpace(settings.Server)
                || String.IsNullOrWhiteSpace(settings.Database)
                || String.IsNullOrWhiteSpace(settings.Username))
            {
                summary.Failed = 1;
                summary.Details = "Thiếu máy chủ, database hoặc tài khoản chỉ đọc.";
                return summary;
            }
            if (!IsSafeIdentifier(settings.ViewName))
            {
                summary.Failed = 1;
                summary.Details = "Tên view không hợp lệ.";
                return summary;
            }

            var service = new AttendanceService();
            var errors = new List<string>();
            using (var connection = CreateConnection(settings))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "SELECT employee_code, card_uid, event_time, event_id FROM " + settings.ViewName + " WHERE event_time >= @since ORDER BY event_time";
                var sinceParameter = command.CreateParameter();
                sinceParameter.ParameterName = "@since";
                sinceParameter.Value = settings.Since == default(DateTime) ? DateTime.Today.AddDays(-1) : settings.Since;
                command.Parameters.Add(sinceParameter);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summary.Total++;
                        try
                        {
                            var employeeCode = reader.IsDBNull(0) ? null : reader.GetValue(0).ToString();
                            var cardUid = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString();
                            var eventTime = Convert.ToDateTime(reader.GetValue(2), CultureInfo.InvariantCulture);
                            var eventId = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString();
                            var result = service.Record(new AttendanceInput
                            {
                                EmployeeCode = employeeCode,
                                CardUid = cardUid,
                                EventTime = eventTime,
                                ExternalEventId = CreateExternalId(
                                    settings, eventId, employeeCode, cardUid, eventTime),
                                Source = "machine-db",
                                DeviceName = settings.Provider + ":" + settings.Server + "/" + settings.Database,
                                RawData = String.Join(";", new[]
                                {
                                    "event_id=" + eventId,
                                    "employee_code=" + employeeCode,
                                    "card_uid=" + cardUid,
                                    "event_time=" + eventTime.ToString("o", CultureInfo.InvariantCulture)
                                })
                            });
                            if (!result.Success)
                            {
                                summary.Failed++;
                                errors.Add("Mã " + (employeeCode ?? "(trống)") + ": " + result.Message);
                            }
                            else if (result.Duplicate)
                                summary.Duplicates++;
                            else
                                summary.Imported++;
                        }
                        catch (Exception ex)
                        {
                            summary.Failed++;
                            errors.Add("Dòng " + summary.Total + ": " + ex.Message);
                        }
                    }
                }
            }
            summary.Details = errors.Count == 0
                ? "Đồng bộ database máy chấm công hoàn tất."
                : String.Join(Environment.NewLine, errors.Take(20));
            AuditService.Log("ATTENDANCE_SYNC_MACHINE", "AttendanceImport",
                settings.Provider + ":" + settings.Server + "/" + settings.Database,
                "View=" + settings.ViewName + "; Total=" + summary.Total
                + "; Imported=" + summary.Imported + "; Duplicates=" + summary.Duplicates
                + "; Failed=" + summary.Failed);
            return summary;
        }

        private static bool IsSafeIdentifier(string value)
        {
            return !String.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[A-Za-z0-9_]+(\\.[A-Za-z0-9_]+)?$");
        }

        private static DbConnection CreateConnection(MachineDatabaseSettings settings)
        {
            if (String.Equals(settings.Provider, "SQL Server", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = settings.Port == 0 || settings.Port == 1433
                        ? settings.Server
                        : settings.Server + "," + settings.Port,
                    InitialCatalog = settings.Database,
                    UserID = settings.Username,
                    Password = settings.Password,
                    IntegratedSecurity = false,
                    ConnectTimeout = 10,
                    Encrypt = false
                };
                return new SqlConnection(builder.ConnectionString);
            }

            return new MySqlConnection(new MySqlConnectionStringBuilder
            {
                Server = settings.Server,
                Port = settings.Port == 0 ? 3306 : settings.Port,
                Database = settings.Database,
                UserID = settings.Username,
                Password = settings.Password,
                SslMode = MySqlSslMode.Preferred,
                ConnectionTimeout = 10
            }.ConnectionString);
        }

        private static string CreateExternalId(MachineDatabaseSettings settings, string eventId,
            string employeeCode, string cardUid, DateTime eventTime)
        {
            var value = String.Join("|", settings.Provider, settings.Server, settings.Database,
                settings.ViewName, eventId, employeeCode, cardUid, eventTime.Ticks);
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", String.Empty);
        }
    }
}
