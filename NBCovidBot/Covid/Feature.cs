using System.Text.Json.Serialization;

namespace NBCovidBot.Covid
{
    public partial class CovidDataProvider
    {
        private class Feature<T>
        {
            [JsonPropertyName("attributes")]
            public T Target { get; set; }
        }
    }
}
