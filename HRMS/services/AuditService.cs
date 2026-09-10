using HRMS.holders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace HRMS.services
{
    internal sealed class AuditEntry
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Details { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    internal static class AuditService
    {
        public static void Log(string action, string entityType, string entityId, string details)
        {
            try
            {
                using (var connection = OpenConnection())
                {
                    LogRequired(connection, null, action, entityType, entityId, details);
                }
            }
            catch (Exception ex)
            {
                // Auditing must not make a completed business action fail.
                Console.Error.WriteLine("Unable to write audit log: " + ex.Message);
            }
        }

        internal static void LogRequired(MySqlConnection connection, MySqlTransaction transaction,
            string action, string entityType, string entityId, string details)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO SystemAuditLogs
(username, action, entityType, entityId, details)
VALUES (@username, @action, @entityType, @entityId, @details)";
                command.Parameters.AddWithValue("@username", Db(AuthHolder.Username, 100));
                command.Parameters.AddWithValue("@action", Db(action, 50));
                command.Parameters.AddWithValue("@entityType", Db(entityType, 50));
                command.Parameters.AddWithValue("@entityId", Db(entityId, 100));
                command.Parameters.AddWithValue("@details", Db(details, 1000));
                command.ExecuteNonQuery();
            }
        }

        public static IList<AuditEntry> GetRecent(int limit)
        {
            if (!AuthorizationService.Can(Permission.ManageRoles))
                throw new UnauthorizedAccessException("Tài khoản không có quyền xem nhật ký hệ thống.");
            limit = Math.Max(1, Math.Min(limit, 2000));
            var result = new List<AuditEntry>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT id, username, action, entityType, entityId, details, createdTime
FROM SystemAuditLogs ORDER BY createdTime DESC LIMIT " + limit;
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        result.Add(new AuditEntry
                        {
                            Id = reader.GetInt64(0),
                            Username = reader.IsDBNull(1) ? String.Empty : reader.GetString(1),
                            Action = reader.GetString(2),
                            EntityType = reader.IsDBNull(3) ? String.Empty : reader.GetString(3),
                            EntityId = reader.IsDBNull(4) ? String.Empty : reader.GetString(4),
                            Details = reader.IsDBNull(5) ? String.Empty : reader.GetString(5),
                            CreatedTime = reader.GetDateTime(6)
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

        private static object Db(string value, int maxLength)
        {
            if (String.IsNullOrWhiteSpace(value))
                return DBNull.Value;
            value = value.Trim();
            return value.Length <= maxLength ? (object)value : value.Substring(0, maxLength);
        }
    }
}
