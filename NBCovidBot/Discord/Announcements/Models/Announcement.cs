using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NBCovidBot.Discord.Announcements.Models
{

    public class Announcement
    {
        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }
    }
}
