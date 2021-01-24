using System.Text.Json.Serialization;

namespace NBCovidBot.Modules.Covid.Models
{
    public class DailyCityCovidInfo
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

        [JsonPropertyName("LastUpdate")]
        public long LastUpdate { get; set; }
    }
}
