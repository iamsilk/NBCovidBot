using System.Text.Json.Serialization;

namespace StebumBot.Modules.Covid.Models
{
    public class UniqueIdField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isSystemMaintained")]
        public bool IsSystemMaintained { get; set; }
    }
}
