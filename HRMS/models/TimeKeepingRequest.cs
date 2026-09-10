namespace HRMS.models
{
    public class TimeKeepingRequest
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string CardUid { get; set; }
        public string Token { get; set; }
        public System.DateTime? EventTime { get; set; }
        public string ExternalEventId { get; set; }
        public string DeviceName { get; set; }
        public string ClientAddress { get; set; }
    }
}
