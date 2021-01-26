using System.Text.Json.Serialization;
// ReSharper disable StringLiteralTypo

namespace NBCovidBot.Covid.Models
{
    public class ProvincePastInfo
    {
        [JsonPropertyName("DATE")]
        public long UnixTimestamp { get; set; }

        [JsonPropertyName("Total")]
        public int Total { get; set; }

        [JsonPropertyName("NewToday")]
        public int NewToday { get; set; }

        [JsonPropertyName("NewRecTday")]
        public int NewRecoveredToday { get; set; }

        [JsonPropertyName("Active")]
        public int Active { get; set; }
    }
}
