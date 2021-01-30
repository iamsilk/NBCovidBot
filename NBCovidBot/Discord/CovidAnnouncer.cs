using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public CovidAnnouncer(AnnouncementsDbContext dbContext,
            CovidDataProvider dataProvider,
            CovidDataFormatter dataFormatter,
            DiscordSocketClient client,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _dataFormatter = dataFormatter;
            _client = client;
            _configuration = configuration;

            dataProvider.RunOnDataUpdated(() => OnDataUpdatedAsync);
        }

        public async Task AnnounceAsync(Embed embed, bool addReactions = true)
        {
            var announcements = await _dbContext.Announcements.ToListAsync();

            foreach (var announcement in announcements)
            {
                var channel =
                    _client.GetGuild(announcement.GuildId)
                        ?.GetTextChannel(announcement.ChannelId);

                if (channel == null) continue;

                var role = channel.Guild.Roles.FirstOrDefault(x => x.Name == _configuration["UserUpdates:RoleName"]);

                var message = await channel.SendMessageAsync(role?.Mention, embed: embed);

                if (message == null) continue;

                await _dataFormatter.AddReactions(message);
            }
        }

        private async Task OnDataUpdatedAsync(bool forced)
        {
            if (forced) return;

            var embed = _dataFormatter.GetEmbed();

            await AnnounceAsync(embed);
        }

        public Task ForceDataAnnouncementAsync() => OnDataUpdatedAsync(false);
    }
}
