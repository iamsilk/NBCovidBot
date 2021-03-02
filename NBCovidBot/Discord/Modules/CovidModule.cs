using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using NBCovidBot.Covid;
using NBCovidBot.Discord.Announcements;
using NBCovidBot.Discord.Announcements.Models;
using NBCovidBot.Discord.Preconditions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NBCovidBot.Discord.Modules
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class CovidModule : ModuleBase<SocketCommandContext>
    {
        private readonly CovidDataProvider _dataProvider;
        private readonly CovidDataFormatter _dataFormatter;
        private readonly AnnouncementsDbContext _dbContext;
        private readonly CovidAnnouncer _covidAnnouncer;

        public CovidModule(CovidDataProvider dataProvider,
            CovidDataFormatter dataFormatter,
            AnnouncementsDbContext dbContext,
            CovidAnnouncer covidAnnouncer)
        {
            _dataProvider = dataProvider;
            _dataFormatter = dataFormatter;
            _dbContext = dbContext;
            _covidAnnouncer = covidAnnouncer;
        }

        [Command("covid")]
        [Summary("View COVID stats")]
        [RequireBotOrServerAdmin]
        public async Task CovidAsync()
        {
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
        [RequireBotAdmin]
        public async Task ForceUpdateAsync()
        {
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
        [RequireBotOrServerAdmin]
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
        [RequireBotAdmin]
        public async Task ForceDailyUpdateAsync()
        {
            await _covidAnnouncer.ForceDataAnnouncementAsync();
        }

        [Command("announce")]
        [Summary("Sends a custom announcement to all daily update channels")]
        [RequireBotAdmin]
        public async Task AnnounceAsync(string title, [Remainder] string message)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle(title)
                .WithColor(Color.Green)
                .WithDescription(message)
                .WithFooter("Bot by Stephen White - https://silk.one/", "https://static.silk.one/avatar.png")
                .WithTimestamp(DateTime.Now);

            await _covidAnnouncer.AnnounceAsync(embedBuilder.Build(), false);
        }

        [Command("imitate")]
        [Summary("Replies with the given message and deletes the original.")]
        [RequireBotAdmin]
        public async Task ImitateAsync([Remainder] string message)
        {
            await ReplyAsync(message);

            await Context.Message.DeleteAsync();
        }
    }
}
