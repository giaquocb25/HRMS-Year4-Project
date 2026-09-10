using HRMS.holders;
using HRMS.models;
using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Configuration;

namespace HRMS.services
{
    internal sealed class AttendanceService
    {
        public AttendanceResult Record(AttendanceInput input)
        {
            if (!AuthorizationService.Can(Permission.RecordAttendance))
                return Failure("Tài khoản không có quyền ghi nhận chấm công.");
            if (input == null)
                return Failure("Dữ liệu chấm công không hợp lệ.");

            var source = Normalize(input.Source, "manual", 30);
            var cardUid = Normalize(input.CardUid, null, 100);
            var employeeCode = Normalize(input.EmployeeCode, null, 30);
            var eventTime = input.EventTime == default(DateTime) ? DateTime.Now : input.EventTime;
            if (eventTime.Kind == DateTimeKind.Utc)
                eventTime = eventTime.ToLocalTime();
            if (eventTime.Year < 2000 || eventTime > DateTime.Now.AddDays(1))
                return Failure("Thời gian chấm công không hợp lệ.");

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var employeeId = ResolveEmployee(connection, transaction, input.EmployeeId, employeeCode, cardUid);
                    if (!employeeId.HasValue)
                        return Failure("Không tìm thấy nhân viên hoặc thẻ chưa được gán.");

                    var employeeName = GetEmployeeName(connection, transaction, employeeId.Value);
                    if (employeeName == null)
                        return Failure("Không tìm thấy nhân viên.");

                    var externalId = Normalize(input.ExternalEventId, null, 160);
                    if (externalId == null)
                        externalId = CreateEventId(source, employeeId.Value, cardUid, eventTime);

                    var inserted = InsertEvent(connection, transaction, employeeId.Value, eventTime, source,
                        externalId, input.DeviceName, input.RawData);
                    if (!inserted)
                    {
                        transaction.Rollback();
                        return new AttendanceResult
                        {
                            Success = true,
                            Duplicate = true,
                            EmployeeId = employeeId.Value,
                            EmployeeName = employeeName,
                            Message = "Sự kiện đã tồn tại, không ghi trùng."
                        };
                    }

                    UpsertDailyAttendance(connection, transaction, employeeId.Value, eventTime);
                    transaction.Commit();
                    return new AttendanceResult
                    {
                        Success = true,
                        EmployeeId = employeeId.Value,
                        EmployeeName = employeeName,
                        Message = "Đã ghi nhận công cho " + employeeName + " lúc " + eventTime.ToString("dd/MM/yyyy HH:mm:ss") + "."
                    };
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    Console.Error.WriteLine("Unable to record attendance: " + ex);
                    return Failure("Không thể ghi nhận công. Hãy kiểm tra kết nối và migration database.");
                }
            }
        }

        public AttendanceResult BindCard(int employeeId, string cardUid)
        {
            if (!AuthorizationService.Can(Permission.ManageEmployees))
                return Failure("Tài khoản không có quyền gán thẻ nhân viên.");
            cardUid = Normalize(cardUid, null, 100);
            if (employeeId <= 0 || cardUid == null)
                return Failure("Nhân viên hoặc mã thẻ không hợp lệ.");

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    using (var employeeCheck = connection.CreateCommand())
                    {
                        employeeCheck.Transaction = transaction;
                        employeeCheck.CommandText = @"SELECT COUNT(*) FROM Employees
WHERE id = @employeeId
  AND (employmentStatus IS NULL OR employmentStatus <> 'Inactive')";
                        employeeCheck.Parameters.AddWithValue("@employeeId", employeeId);
                        if (Convert.ToInt32(employeeCheck.ExecuteScalar()) == 0)
                            return Failure("Không thể gán thẻ cho nhân viên không tồn tại hoặc đã ngừng làm việc.");
                    }

                    using (var remove = connection.CreateCommand())
                    {
                        remove.Transaction = transaction;
                        remove.CommandText = "DELETE FROM EmployeeCards WHERE employeeId = @employeeId OR cardUid = @cardUid";
                        remove.Parameters.AddWithValue("@employeeId", employeeId);
                        remove.Parameters.AddWithValue("@cardUid", cardUid);
                        remove.ExecuteNonQuery();
                    }
                    using (var insert = connection.CreateCommand())
                    {
                        insert.Transaction = transaction;
                        insert.CommandText = "INSERT INTO EmployeeCards (cardUid, employeeId, isActive) VALUES (@cardUid, @employeeId, 1)";
                        insert.Parameters.AddWithValue("@cardUid", cardUid);
                        insert.Parameters.AddWithValue("@employeeId", employeeId);
                        insert.ExecuteNonQuery();
                    }
                    AuditService.LogRequired(connection, transaction, "CARD_BIND", "EmployeeCard",
                        employeeId.ToString(CultureInfo.InvariantCulture), "CardUid=" + cardUid);
                    transaction.Commit();
                    return new AttendanceResult { Success = true, EmployeeId = employeeId, Message = "Đã gán thẻ cho nhân viên." };
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    Console.Error.WriteLine("Unable to assign employee card: " + ex);
                    return Failure("Không thể gán thẻ. Hãy kiểm tra dữ liệu và migration database.");
                }
            }
        }

        public AttendanceResult CorrectDailyAttendance(int employeeId, DateTime workDate,
            TimeSpan arriveTime, TimeSpan leaveTime, string reason)
        {
            if (!AuthorizationService.Can(Permission.CorrectAttendance))
                return Failure("Tài khoản không có quyền điều chỉnh dữ liệu công.");
            reason = Normalize(reason, null, 500);
            if (employeeId <= 0 || workDate.Year < 2000 || workDate.Date > DateTime.Today)
                return Failure("Nhân viên hoặc ngày công không hợp lệ.");
            if (arriveTime < TimeSpan.Zero || arriveTime >= TimeSpan.FromDays(1)
                || leaveTime < TimeSpan.Zero || leaveTime >= TimeSpan.FromDays(1)
                || (leaveTime != TimeSpan.Zero && leaveTime <= arriveTime))
                return Failure("Giờ vào/ra không hợp lệ. Giờ ra phải sau giờ vào trong cùng ngày.");
            if (reason == null || reason.Length < 5)
                return Failure("Lý do điều chỉnh phải có ít nhất 5 ký tự.");

            string employeeName;
            string before;
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    employeeName = GetEmployeeName(connection, transaction, employeeId);
                    if (employeeName == null)
                        return Failure("Không tìm thấy nhân viên.");

                    using (var current = connection.CreateCommand())
                    {
                        current.Transaction = transaction;
                        current.CommandText = @"SELECT arriveTime, leaveTime FROM Timekeeping
WHERE employeeId = @employeeId AND workDate = @workDate
LIMIT 1 FOR UPDATE";
                        current.Parameters.AddWithValue("@employeeId", employeeId);
                        current.Parameters.AddWithValue("@workDate", workDate.Date);
                        using (var reader = current.ExecuteReader())
                            before = reader.Read()
                                ? reader.GetTimeSpan(0).ToString(@"hh\:mm\:ss") + "-"
                                  + reader.GetTimeSpan(1).ToString(@"hh\:mm\:ss")
                                : "(chưa có)";
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"INSERT INTO Timekeeping
(employeeId, arriveTime, leaveTime, workDate, createdTime, updatedTime)
VALUES (@employeeId, @arriveTime, @leaveTime, @workDate, NOW(), NOW())
ON DUPLICATE KEY UPDATE
  arriveTime = VALUES(arriveTime),
  leaveTime = VALUES(leaveTime),
  updatedTime = NOW()";
                        command.Parameters.AddWithValue("@employeeId", employeeId);
                        command.Parameters.AddWithValue("@arriveTime", arriveTime);
                        command.Parameters.AddWithValue("@leaveTime", leaveTime);
                        command.Parameters.AddWithValue("@workDate", workDate.Date);
                        command.ExecuteNonQuery();
                    }
                    AuditService.LogRequired(connection, transaction, "ATTENDANCE_CORRECT", "Timekeeping",
                        employeeId + "@" + workDate.ToString("yyyy-MM-dd"),
                        "Before=" + before + "; After=" + arriveTime.ToString(@"hh\:mm\:ss")
                        + "-" + leaveTime.ToString(@"hh\:mm\:ss") + "; Reason=" + reason);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    Console.Error.WriteLine("Unable to correct attendance: " + ex);
                    return Failure("Không thể điều chỉnh công. Dữ liệu cũ đã được giữ nguyên.");
                }
            }

            return new AttendanceResult
            {
                Success = true,
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                Message = "Đã điều chỉnh công cho " + employeeName + " ngày "
                    + workDate.ToString("dd/MM/yyyy") + "."
            };
        }

        public IList<AttendanceEventView> GetRecentEvents(int limit)
        {
            if (!AuthorizationService.Can(Permission.ViewAttendance))
                throw new UnauthorizedAccessException("Tài khoản không có quyền xem nhật ký chấm công.");
            limit = Math.Max(1, Math.Min(limit, 1000));
            var result = new List<AttendanceEventView>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT e.id, p.name, e.eventTime, e.source, e.deviceName, e.createdBy
FROM AttendanceEvents e
INNER JOIN Employees p ON p.id = e.employeeId
ORDER BY e.eventTime DESC
LIMIT " + limit;
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(new AttendanceEventView
                        {
                            Id = reader.GetInt64(0),
                            EmployeeName = reader.GetString(1),
                            EventTime = reader.GetDateTime(2),
                            Source = reader.GetString(3),
                            DeviceName = reader.IsDBNull(4) ? String.Empty : reader.GetString(4),
                            CreatedBy = reader.IsDBNull(5) ? String.Empty : reader.GetString(5)
                        });
            }
            return result;
        }

        private static MySqlConnection OpenConnection()
        {
            var connection = new MySqlConnection(DBManagement.GetProviderConnectionString(AuthHolder.Username, AuthHolder.Password));
            connection.Open();
            return connection;
        }

        private static int? ResolveEmployee(MySqlConnection connection, MySqlTransaction transaction,
            int? employeeId, string employeeCode, string cardUid)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                if (cardUid != null)
                {
                    command.CommandText = @"SELECT c.employeeId
FROM EmployeeCards c
INNER JOIN Employees e ON e.id = c.employeeId
WHERE c.cardUid = @cardUid
  AND c.isActive = 1
  AND (e.employmentStatus IS NULL OR e.employmentStatus <> 'Inactive')
LIMIT 1";
                    command.Parameters.AddWithValue("@cardUid", cardUid);
                    var cardValue = command.ExecuteScalar();
                    if (cardValue != null)
                        return Convert.ToInt32(cardValue, CultureInfo.InvariantCulture);
                    command.Parameters.Clear();
                }

                if (employeeCode != null)
                {
                    command.CommandText = @"SELECT id FROM Employees
WHERE employeeCode = @employeeCode
  AND (employmentStatus IS NULL OR employmentStatus <> 'Inactive')
LIMIT 1";
                    command.Parameters.AddWithValue("@employeeCode", employeeCode);
                    var codeValue = command.ExecuteScalar();
                    if (codeValue != null)
                        return Convert.ToInt32(codeValue, CultureInfo.InvariantCulture);
                    command.Parameters.Clear();
                }

                if (employeeId.HasValue && employeeId.Value > 0)
                {
                    command.CommandText = @"SELECT id FROM Employees
WHERE id = @employeeId
  AND (employmentStatus IS NULL OR employmentStatus <> 'Inactive')
LIMIT 1";
                    command.Parameters.AddWithValue("@employeeId", employeeId.Value);
                    var idValue = command.ExecuteScalar();
                    if (idValue != null)
                        return Convert.ToInt32(idValue, CultureInfo.InvariantCulture);
                }
                return null;
            }
        }

        private static string GetEmployeeName(MySqlConnection connection, MySqlTransaction transaction, int employeeId)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT name FROM Employees WHERE id = @employeeId LIMIT 1";
                command.Parameters.AddWithValue("@employeeId", employeeId);
                var value = command.ExecuteScalar();
                return value == null ? null : value.ToString();
            }
        }

        private static bool InsertEvent(MySqlConnection connection, MySqlTransaction transaction, int employeeId,
            DateTime eventTime, string source, string externalId, string deviceName, string rawData)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO AttendanceEvents
(employeeId, eventTime, source, externalEventId, deviceName, rawData, createdBy)
VALUES (@employeeId, @eventTime, @source, @externalId, @deviceName, @rawData, @createdBy)";
                command.Parameters.AddWithValue("@employeeId", employeeId);
                command.Parameters.AddWithValue("@eventTime", eventTime);
                command.Parameters.AddWithValue("@source", source);
                command.Parameters.AddWithValue("@externalId", externalId);
                command.Parameters.AddWithValue("@deviceName", (object)Normalize(deviceName, null, 100) ?? DBNull.Value);
                command.Parameters.AddWithValue("@rawData", (object)Normalize(rawData, null, 4000) ?? DBNull.Value);
                command.Parameters.AddWithValue("@createdBy", AuthHolder.Username);
                try
                {
                    return command.ExecuteNonQuery() > 0;
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062)
                        return false;
                    throw;
                }
            }
        }

        private static void UpsertDailyAttendance(MySqlConnection connection, MySqlTransaction transaction,
            int employeeId, DateTime eventTime)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                // MySQL evaluates single-table UPDATE assignments from left to right. Compute
                // leaveTime before replacing arriveTime so an earlier event imported later still
                // preserves the previous (later) scan as the departure time.
                command.CommandText = @"INSERT INTO Timekeeping
(employeeId, arriveTime, leaveTime, workDate, createdTime, updatedTime)
VALUES (@employeeId, TIME(@eventTime), '00:00:00', @workDate, NOW(), NOW())
ON DUPLICATE KEY UPDATE
  leaveTime = CASE
    WHEN TIME_TO_SEC(TIMEDIFF(
      GREATEST(arriveTime, leaveTime, TIME(@eventTime)),
      LEAST(arriveTime, TIME(@eventTime)))) >= @minimumCheckoutSeconds
      THEN GREATEST(arriveTime, leaveTime, TIME(@eventTime))
    ELSE '00:00:00'
  END,
  arriveTime = LEAST(arriveTime, TIME(@eventTime)),
  updatedTime = NOW()";
                command.Parameters.AddWithValue("@employeeId", employeeId);
                command.Parameters.AddWithValue("@eventTime", eventTime);
                command.Parameters.AddWithValue("@workDate", eventTime.Date);
                command.Parameters.AddWithValue("@minimumCheckoutSeconds", GetMinimumCheckoutMinutes() * 60);
                command.ExecuteNonQuery();
            }
        }

        private static int GetMinimumCheckoutMinutes()
        {
            int value;
            return Int32.TryParse(ConfigurationManager.AppSettings["MinimumCheckoutMinutes"], out value)
                && value >= 0 && value <= 240 ? value : 5;
        }

        private static string CreateEventId(string source, int employeeId, string cardUid, DateTime eventTime)
        {
            var payload = source + "|" + employeeId + "|" + cardUid + "|" + eventTime.ToUniversalTime().Ticks;
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", String.Empty);
        }

        private static string Normalize(string value, string fallback, int maxLength)
        {
            var result = String.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            return result != null && result.Length > maxLength ? result.Substring(0, maxLength) : result;
        }

        private static AttendanceResult Failure(string message)
        {
            return new AttendanceResult { Success = false, Message = message };
        }
    }
}
