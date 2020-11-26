using System.Text.Json.Serialization;

namespace StebumBot.Modules.Covid.Models
{
    public class SpatialReference
    {
        [JsonPropertyName("wkid")]
        public int Wkid { get; set; }

        [JsonPropertyName("latestWkid")]
        public int LatestWkid { get; set; }
    }
}
