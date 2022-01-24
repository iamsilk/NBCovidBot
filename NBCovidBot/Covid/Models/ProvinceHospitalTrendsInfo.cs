using System.Text.Json.Serialization;

namespace NBCovidBot.Covid.Models
{
    public class ProvinceHospitalTrendsInfo
    {
        [JsonPropertyName("Hospitalizations")]
        public int CurrentHospitalizations { get; set; }

        [JsonPropertyName("ICU")]
        public int CurrentICU { get; set; }
    }
}
