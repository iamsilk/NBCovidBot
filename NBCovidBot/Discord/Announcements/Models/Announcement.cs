using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NBCovidBot.Discord.Announcements.Models
{

    public class Announcement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public ulong Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }
    }
}
