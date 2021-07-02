using System.Text.Json.Serialization;

namespace NBCovidBot.Covid.Models
{
    public class ProvinceVaccineInfo
    {
        [JsonPropertyName("TotalReceived")]
        public int TotalReceived { get; set; }

        [JsonPropertyName("TotalAdmin")]
        public int TotalAdministered { get; set; }

        [JsonPropertyName("TotalDoseWeek")]
        public int TotalDoseWeek { get; set; }

        [JsonPropertyName("PopOneDose")]
        public int PopulationOneDose { get; set; }

        [JsonPropertyName("PercentOneDose")]
        public double PercentOneDose { get; set; }

        [JsonPropertyName("PopSecondDose")]
        public int PopulationSecondDose { get; set; }

        [JsonPropertyName("PercentSecondDose")]
        public double PercentSecondDose { get; set; }

        [JsonPropertyName("LastUpdateText")]
        public string LastUpdateText { get; set; }
    }
}
