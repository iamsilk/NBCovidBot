namespace NBCovidBot.Covid.Models
{
    public class HealthZone
    {
        public string Title { get; set; }

        public int ZoneNumber { get; set; }

        public HealthZone(string title, int zoneNumber)
        {
            Title = title;
            ZoneNumber = zoneNumber;
        }
    }
}
