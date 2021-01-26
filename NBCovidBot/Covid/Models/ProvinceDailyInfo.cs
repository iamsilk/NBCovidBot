using System.Text.Json.Serialization;
// ReSharper disable StringLiteralTypo

namespace NBCovidBot.Covid.Models
{
    public class ProvinceDailyInfo : ZoneDailyInfo
    {
        [JsonPropertyName("TravelRel")]
        public int TravelRelated { get; set; }

        [JsonPropertyName("ClsContct")]
        public int CloseContact { get; set; }

        [JsonPropertyName("CommTrnsmsn")]
        public int CommTransmission { get; set; }

        [JsonPropertyName("Hospitalised")]
        public int Hospitalized { get; set; }

        [JsonPropertyName("ICU")]
        public int ICU { get; set; }

        [JsonPropertyName("TotalTests")]
        public int TotalTests { get; set; }

        [JsonPropertyName("UnderInves")]
        public int UnderInvestigation { get; set; }
    }
}
