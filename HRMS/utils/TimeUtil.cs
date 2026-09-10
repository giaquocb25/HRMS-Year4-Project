using System;
using System.Configuration;
using System.Globalization;
namespace HRMS.utils
{
    internal static class TimeUtil
    {
        public static int StandardWorkingDays
        {
            get
            {
                int value;
                return Int32.TryParse(ConfigurationManager.AppSettings["StandardWorkingDays"], out value)
                    && value >= 1 && value <= 31 ? value : 22;
            }
        }

        public static double FullWorkdayHours
        {
            get
            {
                double value;
                return Double.TryParse(ConfigurationManager.AppSettings["FullWorkdayHours"],
                    NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    && value > 0d && value <= 24d ? value : 8d;
            }
        }

        public static decimal MandatoryInsuranceRate
        {
            get
            {
                decimal value;
                return Decimal.TryParse(ConfigurationManager.AppSettings["MandatoryInsuranceRate"],
                    NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    && value >= 0m && value <= 1m ? value : 0.105m;
            }
        }

        public static string GetCurrentMonth()
        {
            return DateTime.Today.ToString("MM/yyyy");
        }

        public static DateTime GetCurrentMonthStart()
        {
            var today = DateTime.Today;
            return new DateTime(today.Year, today.Month, 1);
        }

        public static bool IsFullWorkday(Timekeeping item)
        {
            if (item == null || item.leaveTime <= item.arriveTime)
                return false;

            return item.leaveTime.Subtract(item.arriveTime).TotalHours >= FullWorkdayHours;
        }
    }
}
