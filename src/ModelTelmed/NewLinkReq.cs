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
        public long ClientId { get; set; }
        public string ClientCode { get; set; }
        public string ClientDescription { get; set; }
        public string ShamanAffiliateNumber { get; set; }
        public long Address_LocationId { get; set; }
        public string Address_Location { get; set; }
        public string Address_Street { get; set; }
        public string Address_PostalCode { get; set; }
        public string Address_Number { get; set; }
        public string Address_Floor { get; set; }
        public string Address_Dpto { get; set; }
        public decimal Address_Lat { get; set; }
        public decimal Address_Lng { get; set; }
        public string Address_BetweenStreet1 { get; set; }
        public string Address_BetweenStreet2 { get; set; }
        public string Address_Reference { get; set; }

    }
}
