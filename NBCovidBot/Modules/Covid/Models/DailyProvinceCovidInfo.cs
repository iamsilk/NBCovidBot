using System.Text.Json.Serialization;

namespace NBCovidBot.Modules.Covid.Models
{
    public class DailyProvinceCovidInfo
    {
        [JsonPropertyName("RecoveryPhase")]
        public string RecoveryPhase { get; set; }

        [JsonPropertyName("TotalCases")]
        public int TotalCases { get; set; }

        [JsonPropertyName("NewToday")]
        public int NewToday { get; set; }

        [JsonPropertyName("ActiveCases")]
        public int ActiveCases { get; set; }

        [JsonPropertyName("Recovered")]
        public int Recovered { get; set; }

        [JsonPropertyName("Deaths")]
        public int Deaths { get; set; }

        [JsonPropertyName("TotalTests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("Hospitalised")]
        public int Hospitalized { get; set; }

        [JsonPropertyName("ICU")]
        public int ICU { get; set; }

        [JsonPropertyName("TravelRel")]
        public int TravelRel { get; set; }

        [JsonPropertyName("ClsContct")]
        public int CloseContact { get; set; }

        [JsonPropertyName("CommTrnsmsn")]
        public int CommTransmission { get; set; }

        [JsonPropertyName("UnderInves")]
        public int UnderInvestigation { get; set; }

        [JsonPropertyName("LastUpdate")]
        public long LastUpdate { get; set; }
    }
}
