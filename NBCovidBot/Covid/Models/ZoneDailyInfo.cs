using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NBCovidBot.Covid.Models
{
    public class ZoneDailyInfo
    {
        //[Key]
        [JsonIgnore]
        public int ZoneNumber { get; set; }

        //[Key]
        [JsonPropertyName("LastUpdate")]
        public long LastUpdate { get; set; }

        [JsonIgnore]
        public string ZoneTitle { get; set; }

        [JsonPropertyName("ActiveCases")]
        public int ActiveCases { get; set; }

        [JsonPropertyName("NewToday")]
        public int NewToday { get; set; }

        [JsonPropertyName("TotalCases")]
        public int TotalCases { get; set; }

        [JsonPropertyName("Recovered")]
        public int Recovered { get; set; }

        [JsonPropertyName("Deaths")]
        public int Deaths { get; set; }
    }
}
