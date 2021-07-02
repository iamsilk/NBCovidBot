using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace NBCovidBot.Covid.Models
{
    public class ZoneDailyInfo
    {
        //[Key]
        [JsonIgnore]
        public int ZoneNumber { get; set; }

        //[Key]
        [JsonPropertyName("LastUpdateText")]
        public string LastUpdateText
        {
            get => DateTimeOffset.UnixEpoch.AddSeconds(LastUpdate).ToString("M/d/yyyy");
            set => LastUpdate = DateTimeOffset.ParseExact(value, "M/d/yyyy", new CultureInfo("en-US"))
                .ToUnixTimeSeconds();
        }

        [JsonIgnore]
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
