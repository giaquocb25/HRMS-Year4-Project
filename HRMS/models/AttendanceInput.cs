using System;

namespace HRMS.models
{
    internal sealed class AttendanceInput
    {
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string CardUid { get; set; }
        public DateTime EventTime { get; set; }
        public string Source { get; set; }
        public string ExternalEventId { get; set; }
        public string DeviceName { get; set; }
        public string RawData { get; set; }
    }

    public sealed class AttendanceResult
    {
        public bool Success { get; set; }
        public bool Duplicate { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Message { get; set; }
    }

    internal sealed class AttendanceImportSummary
    {
        public int Total { get; set; }
        public int Imported { get; set; }
        public int Duplicates { get; set; }
        public int Failed { get; set; }
        public string Details { get; set; }
    }

    internal sealed class AttendanceEventView
    {
        public long Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime EventTime { get; set; }
        public string Source { get; set; }
        public string DeviceName { get; set; }
        public string CreatedBy { get; set; }
    }
}
