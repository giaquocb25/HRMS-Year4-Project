using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using MySql.Data.MySqlClient;

namespace HRMS
{
    internal static class DBManagement
    {
        private const string Metadata = "res://*/HRMS.csdl|res://*/HRMS.ssdl|res://*/HRMS.msl";
        private const string Provider = "MySql.Data.MySqlClient";

        public static string getConnection(string userId, string pass)
        {
            var entityBuilder = new EntityConnectionStringBuilder
            {
                Metadata = Metadata,
                Provider = Provider,
                ProviderConnectionString = GetProviderConnectionString(userId, pass)
            };

            return entityBuilder.ConnectionString;
        }

        public static string GetProviderConnectionString(string userId, string pass)
        {
            if (String.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Database username is required.", "userId");

            return new MySqlConnectionStringBuilder
            {
                Server = GetSetting("DatabaseServer", "localhost"),
                Port = GetPort(),
                Database = GetSetting("DatabaseName", "hmrs"),
                UserID = userId.Trim(),
                Password = pass ?? String.Empty,
                PersistSecurityInfo = false,
                ConvertZeroDateTime = true,
                AllowZeroDateTime = false,
                SslMode = GetSslMode(),
                AllowPublicKeyRetrieval = GetBooleanSetting(
                    "HRMS_ALLOW_PUBLIC_KEY_RETRIEVAL",
                    "DatabaseAllowPublicKeyRetrieval",
                    false)
            }.ConnectionString;
        }

        private static MySqlSslMode GetSslMode()
        {
            var value = GetOverrideSetting("HRMS_DATABASE_SSL_MODE", "DatabaseSslMode", "Preferred");
            MySqlSslMode mode;
            return Enum.TryParse(value, true, out mode) ? mode : MySqlSslMode.Preferred;
        }

        private static bool GetBooleanSetting(string environmentKey, string settingKey, bool defaultValue)
        {
            bool value;
            return Boolean.TryParse(GetOverrideSetting(environmentKey, settingKey, defaultValue.ToString()), out value)
                ? value
                : defaultValue;
        }

        private static string GetOverrideSetting(string environmentKey, string settingKey, string defaultValue)
        {
            var environmentValue = Environment.GetEnvironmentVariable(environmentKey);
            return String.IsNullOrWhiteSpace(environmentValue)
                ? GetSetting(settingKey, defaultValue)
                : environmentValue.Trim();
        }

        private static string GetSetting(string key, string defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static uint GetPort()
        {
            uint port;
            return UInt32.TryParse(GetSetting("DatabasePort", "3306"), out port) ? port : 3306;
        }
    }
}
