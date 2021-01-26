using System.Text.Json.Serialization;
// ReSharper disable StringLiteralTypo

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider
    {
        private class SpatialReference
        {
            [JsonPropertyName("wkid")]
            public int Wkid { get; set; }

            [JsonPropertyName("latestWkid")]
            public int LatestWkid { get; set; }
        }
    }
}