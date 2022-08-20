using System;

namespace ModelTelmed
{
    public class ConferenceBO
    {
        public int ConferenceId { get; set; }
        public int UserId { get; set; }
        public int? DoctorId { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCancelled { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string UrlTelemedicina { get; set; }
        public dynamic User { get; set; }
        public bool IsChatOnly { get; set; }
        public int? ConferenceTypeID { get; set; }
        public dynamic ConferenceType { get; set; }
        public long? ShamanIncidenteId { get; set; }
        public dynamic Company { get; set; }
        public int? CompanyId { get; set; }
        public string Symptom { get; set; }
        public DateTime? ScheduledAppointment { get; set; }
    }
}