using System.Text.Json.Serialization;

namespace NBCovidBot.Modules.Covid.Models
{
    public class UniqueIdField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isSystemMaintained")]
        public bool IsSystemMaintained { get; set; }
    }
}
