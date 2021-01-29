using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NBCovidBot.Covid;
using NBCovidBot.Discord.Announcements;
using NBCovidBot.Discord.Announcements.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NBCovidBot.Discord.Modules
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CovidModule : ModuleBase<SocketCommandContext>
    {
        private readonly CovidDataProvider _dataProvider;
        private readonly CovidDataFormatter _dataFormatter;
        private readonly IConfiguration _configuration;
        private readonly AnnouncementsDbContext _dbContext;
        private readonly CovidAnnouncer _covidAnnouncer;

        public CovidModule(CovidDataProvider dataProvider,
            CovidDataFormatter dataFormatter,
            IConfiguration configuration,
            AnnouncementsDbContext dbContext,
            CovidAnnouncer covidAnnouncer)
        {
            _dataProvider = dataProvider;
            _dataFormatter = dataFormatter;
            _configuration = configuration;
            _dbContext = dbContext;
            _covidAnnouncer = covidAnnouncer;
        }

        private bool IsBotAdmin()
        {
            var admins = _configuration.GetSection("Admins").Get<string[]>();

            return admins != null && admins.Contains(Context.User.ToString());
        }

        [Command("covid")]
        [Summary("View COVID stats")]
        public async Task CovidAsync()
        {
            if (!IsBotAdmin()) return;

            var embed = _dataFormatter.GetEmbed();

            if (embed == null)
            {
                await ReplyAsync("Couldn't retrieve recent COVID-19 data.");
                return;
            }

            var message = await ReplyAsync(embed: embed);

            if (message == null) return;

            await _dataFormatter.AddReactions(message);
        }

        [Command("forceupdate")]
        [Summary("Force update of COVID stats")]
        public async Task ForceUpdateAsync()
        {
            if (!IsBotAdmin()) return;
            
            var success = await _dataProvider.ForceUpdateData();

            if (success)
            {
                await ReplyAsync("Successfully forced data update.");
            }
            else
            {
                await ReplyAsync("Error occurred when forcing data update.");
            }
        }
        
        [Command("dailyupdate")]
        [Summary("Adds/removes current channel to/from list of daily updated channels")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DailyUpdateAsync()
        {
            var existingRecord = await _dbContext.Announcements.FirstOrDefaultAsync(x =>
                x.GuildId == Context.Guild.Id && x.ChannelId == Context.Channel.Id);

            if (existingRecord != null)
            {
                _dbContext.Announcements.Remove(existingRecord);
            }
            else
            {
                await _dbContext.Announcements.AddAsync(new Announcement
                {
                    GuildId = Context.Guild.Id,
                    ChannelId = Context.Channel.Id
                });
            }

            await _dbContext.SaveChangesAsync();

            if (existingRecord == null)
            {
                await ReplyAsync("This channel has been configured for daily COVID updates.");
            }
            else
            {
                await ReplyAsync("This channel has been unsubscribed from daily COVID updates.");
            }
        }

        [Command("forcedailyupdate")]
        [Summary("Forces a daily update among all subscribed channels")]
        public async Task ForceDailyUpdateAsync()
        {
            if (!IsBotAdmin()) return;

            await _covidAnnouncer.ForceAnnouncementAsync();
        }
    }
}
