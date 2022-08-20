using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTelmed
{
    public class NewLinkReq
    {
        public long ShamanIncidenteID { get; set; }
        public long UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long DNI { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string FecNacimiento { get; set; }
        public string Email { get; set; }
        public string Serial { get; set; }
        public string Symptom { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public string DailyFrequency { get; set; }
        public string HourlyFrequencyId { get; set; }
        public string surveyID { get; set; }

    }
}
