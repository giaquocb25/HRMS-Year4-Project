using System;
namespace HRMS.holders
{
    internal static class TokenHolder
    {
        private static readonly object SyncRoot = new object();
        private static string token;
        private static DateTime expiresAtUtc;

        public static string Generate(TimeSpan lifetime)
        {
            lock (SyncRoot)
            {
                if (!String.IsNullOrWhiteSpace(token) && DateTime.UtcNow <= expiresAtUtc)
                    return token;

                token = Guid.NewGuid().ToString("N");
                expiresAtUtc = DateTime.UtcNow.Add(lifetime);
                return token;
            }
        }

        public static bool IsValid(string candidate)
        {
            lock (SyncRoot)
            {
                return !String.IsNullOrWhiteSpace(candidate)
                    && DateTime.UtcNow <= expiresAtUtc
                    && String.Equals(token, candidate, StringComparison.Ordinal);
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                token = null;
                expiresAtUtc = DateTime.MinValue;
            }
        }
    }
}
