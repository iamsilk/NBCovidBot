
using System.Text.Json.Serialization;

namespace StebumBot.Modules.Covid.Models
{
    public class PastCovidInfo
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
