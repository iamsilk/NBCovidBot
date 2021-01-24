using System.Text.Json.Serialization;

namespace NBCovidBot.Modules.Covid.Models
{
    public class Feature<T>
    {
        [JsonPropertyName("attributes")]
        public T Target { get; set; }
    }
}
