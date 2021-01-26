using System.Text.Json.Serialization;

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider
    {
        private class UniqueIdField
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("isSystemMaintained")]
            public bool IsSystemMaintained { get; set; }
        }
    }
}