using System.Text.Json.Serialization;

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider
    {
        private class Field
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("alias")]
            public string Alias { get; set; }

            [JsonPropertyName("sqlType")]
            public string SqlType { get; set; }

            [JsonPropertyName("length")]
            public int Length { get; set; }

            [JsonPropertyName("domain")]
            public object Domain { get; set; }

            [JsonPropertyName("defaultValue")]
            public object DefaultValue { get; set; }
        }
    }
}