using HRMS.utils;
using System;

namespace HRMS
{
    public partial class Timekeeping
    {
        public double workedHours
        {
            get
            {
                if (leaveTime <= arriveTime || leaveTime == TimeSpan.Zero)
                    return 0;
                return Math.Round((leaveTime - arriveTime).TotalHours, 2);
            }
        }

        public string attendanceStatus
        {
            get
            {
                if (leaveTime == TimeSpan.Zero)
                    return "Chưa chấm ra";
                return TimeUtil.IsFullWorkday(this) ? "Đủ công" : "Thiếu giờ";
            }
        }
    }
}
