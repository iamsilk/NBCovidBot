using System.Text.Json.Serialization;

namespace NBCovidBot.Covid.Models
{
    public class ZoneRecoveryPhaseInfo
    {
        [JsonPropertyName("community_name")]
        public string CommunityName { get; set; }

        [JsonPropertyName("RecoveryPhase")]
        public string RecoveryPhase { get; set; }

        [JsonPropertyName("HealthZone")]
        public string HealthZoneStr
        {
            get => HealthZone.ToString();
            set => HealthZone = int.Parse(value);
        }

        [JsonIgnore]
        public int HealthZone { get; set; }
    }
}
