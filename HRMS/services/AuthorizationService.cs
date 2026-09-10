using HRMS.holders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HRMS.services
{
    internal sealed class AccountDisabledException : UnauthorizedAccessException
    {
        public AccountDisabledException()
            : base("Tài khoản đã bị vô hiệu hóa trong hệ thống HRMS.")
        {
        }
    }

    internal enum Permission
    {
        ViewEmployees,
        ManageEmployees,
        ViewAttendance,
        RecordAttendance,
        ImportAttendance,
        CorrectAttendance,
        ViewPayroll,
        CalculatePayroll,
        ManageRoles
    }

    internal static class AuthorizationService
    {
        public static UserRole LoadRole(string username, string password)
        {
            using (var connection = new MySqlConnection(DBManagement.GetProviderConnectionString(username, password)))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "SELECT role, isActive FROM UserRoles WHERE username = @username LIMIT 1";
                command.Parameters.AddWithValue("@username", username);
                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return UserRole.Viewer;
                        if (!reader.GetBoolean(1))
                            throw new AccountDisabledException();

                        UserRole role;
                        return Enum.TryParse(reader.GetString(0), true, out role)
                            ? role
                            : UserRole.Viewer;
                    }
                }
                catch (MySqlException ex)
                {
                    // Until the migration is installed, use least privilege.
                    if (ex.Number == 1146)
                        return UserRole.Viewer;
                    throw;
                }
            }
        }

        public static bool Can(Permission permission)
        {
            var role = AuthHolder.Role;
            if (role == UserRole.Admin)
                return true;

            switch (permission)
            {
                case Permission.ViewEmployees:
                    return true;
                case Permission.ManageEmployees:
                    return role == UserRole.HR;
                case Permission.CorrectAttendance:
                    return role == UserRole.HR;
                case Permission.ViewAttendance:
                    return role == UserRole.HR || role == UserRole.Timekeeper || role == UserRole.Payroll;
                case Permission.RecordAttendance:
                case Permission.ImportAttendance:
                    return role == UserRole.Timekeeper;
                case Permission.ViewPayroll:
                case Permission.CalculatePayroll:
                    return role == UserRole.Payroll;
                case Permission.ManageRoles:
                    return false;
                default:
                    return false;
            }
        }

        public static IList<RoleAssignment> GetAssignments()
        {
            if (!Can(Permission.ManageRoles))
                throw new UnauthorizedAccessException("Tài khoản không có quyền xem phân quyền.");
            var result = new List<RoleAssignment>();
            using (var connection = OpenCurrentConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT username, role, isActive FROM UserRoles ORDER BY username";
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(new RoleAssignment
                        {
                            Username = reader.GetString(0),
                            Role = reader.GetString(1),
                            IsActive = reader.GetBoolean(2)
                        });
            }
            return result;
        }

        public static void SaveAssignment(RoleAssignment assignment)
        {
            if (!Can(Permission.ManageRoles))
                throw new UnauthorizedAccessException("Tài khoản không có quyền sửa phân quyền.");
            if (assignment == null || String.IsNullOrWhiteSpace(assignment.Username))
                throw new ArgumentException("Tên đăng nhập không hợp lệ.");
            var username = assignment.Username.Trim();
            if (username.Length > 100)
                throw new ArgumentException("Tên đăng nhập không được vượt quá 100 ký tự.");
            var roleName = assignment.Role == null ? null : assignment.Role.Trim();
            UserRole parsedRole;
            if (!Enum.TryParse(roleName, true, out parsedRole)
                || !String.Equals(Enum.GetName(typeof(UserRole), parsedRole), roleName,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Vai trò không hợp lệ.");

            using (var connection = OpenCurrentConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (!assignment.IsActive || parsedRole != UserRole.Admin)
                {
                    using (var adminCheck = connection.CreateCommand())
                    {
                        adminCheck.Transaction = transaction;
                        adminCheck.CommandText = @"SELECT COUNT(*) FROM UserRoles
WHERE role = 'Admin' AND isActive = 1 AND username <> @username";
                        adminCheck.Parameters.AddWithValue("@username", username);
                        if (Convert.ToInt32(adminCheck.ExecuteScalar()) == 0)
                            throw new InvalidOperationException(
                                "Không thể vô hiệu hóa hoặc hạ quyền quản trị viên cuối cùng.");
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"INSERT INTO UserRoles (username, role, isActive)
VALUES (@username, @role, @active)
ON DUPLICATE KEY UPDATE role = VALUES(role), isActive = VALUES(isActive), updatedTime = CURRENT_TIMESTAMP";
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@role", parsedRole.ToString());
                    command.Parameters.AddWithValue("@active", assignment.IsActive);
                    command.ExecuteNonQuery();
                }
                AuditService.LogRequired(connection, transaction, "ROLE_UPDATE", "UserRole", username,
                    parsedRole + "; active=" + assignment.IsActive);
                transaction.Commit();
            }
        }

        private static MySqlConnection OpenCurrentConnection()
        {
            var connection = new MySqlConnection(DBManagement.GetProviderConnectionString(AuthHolder.Username, AuthHolder.Password));
            connection.Open();
            return connection;
        }
    }

    internal sealed class RoleAssignment
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}
