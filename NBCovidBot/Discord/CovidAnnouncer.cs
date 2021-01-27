using Discord.WebSocket;
using NBCovidBot.Covid;
using NBCovidBot.Discord.Announcements;
using System.Linq;
using System.Threading.Tasks;

namespace NBCovidBot.Discord
{
    public class CovidAnnouncer
    {
        private readonly AnnouncementsDbContext _dbContext;
        private readonly CovidDataFormatter _dataFormatter;
        private readonly DiscordSocketClient _client;

        public CovidAnnouncer(AnnouncementsDbContext dbContext,
            CovidDataProvider dataProvider,
            CovidDataFormatter dataFormatter,
            DiscordSocketClient client)
        {
            _dbContext = dbContext;
            _dataFormatter = dataFormatter;
            _client = client;

            dataProvider.RunOnDataUpdated(() => OnDataUpdatedAsync);
        }

        private async Task OnDataUpdatedAsync(bool forced)
        {
            var announcements = await _dbContext.Announcements.ToListAsync();

            var embed = _dataFormatter.GetEmbed();

            foreach (var announcement in announcements)
            {
                var channel =
                    _client.GetGuild(announcement.GuildId)
                        ?.GetTextChannel(announcement.ChannelId);

                if (channel == null) continue;

                await channel.SendMessageAsync(embed: embed);
            }
        }

        public Task ForceAnnouncementAsync() => OnDataUpdatedAsync(false);
    }
}
