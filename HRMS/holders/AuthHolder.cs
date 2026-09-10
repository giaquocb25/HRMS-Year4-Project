using System;
namespace HRMS.holders
{
    internal enum UserRole
    {
        Viewer,
        Timekeeper,
        Payroll,
        HR,
        Admin
    }

    internal static class AuthHolder
    {
        public static string Username { get; private set; }
        public static string Password { get; private set; }
        public static UserRole Role { get; private set; }

        public static void SetCredentials(string username, string password)
        {
            Username = (username ?? System.String.Empty).Trim();
            Password = password ?? System.String.Empty;
            Role = UserRole.Viewer;
        }

        public static void SetRole(UserRole role)
        {
            Role = role;
        }

        public static void Clear()
        {
            Username = System.String.Empty;
            Password = System.String.Empty;
            Role = UserRole.Viewer;
        }
    }
}
