using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBCovidBot.Commands;
using NBCovidBot.Covid;
using NBCovidBot.Discord.Announcements;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBCovidBot.Discord
{
    public class DiscordBot : IHostedService
    {
        private readonly Runtime _runtime;
        private readonly ILogger<DiscordBot> _logger;
        private readonly IConfiguration _configuration;
        private readonly CommandHandler _commandHandler;
        private readonly DiscordSocketClient _client;
        private readonly AnnouncementsDbContext _announcementsDbContext;
        private readonly CovidDataDbContext _covidDataDbContext;

        public DiscordBot(
            Runtime runtime,
            ILogger<DiscordBot> logger,
            IConfiguration configuration,
            CommandHandler commandHandler,
            DiscordSocketClient client,
            AnnouncementsDbContext announcementsDbContext,
            CovidDataDbContext covidDataDbContext)
        {
            _runtime = runtime;
            _logger = logger;
            _configuration = configuration;
            _commandHandler = commandHandler;
            _client = client;
            _announcementsDbContext = announcementsDbContext;
            _covidDataDbContext = covidDataDbContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var token = _configuration["Token"];

            if (string.IsNullOrWhiteSpace(token) || token == "CHANGEME")
            {
                _logger.LogCritical("A token must be specified in the config file.");

                // We must close the application by directly stopping the runtime
                // as if we call Environment.Exit, the method won't return until the
                // application has exited. The application however won't exit until this
                // method has returned resulting the application locking.

                // ReSharper disable once MethodSupportsCancellation
                await _runtime.Host.StopAsync();

                return;
            }

            _client.Log += OnLog;
            _client.ReactionAdded += OnReactionAdded;

            await _announcementsDbContext.Database.MigrateAsync(cancellationToken);
            await _covidDataDbContext.Database.MigrateAsync(cancellationToken);

            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();

            _client.Log -= OnLog;
        }

        private Task OnLog(LogMessage arg)
        {
            _logger.LogInformation(arg.Message);

            return Task.CompletedTask;
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var message = await cacheableMessage.DownloadAsync();

            // Check if reaction is for one of the bot's messages
            if (_client.CurrentUser.Id != message.Author.Id) return;

            // Check if reaction is from bot
            if (_client.CurrentUser.Id == reaction.UserId) return;
            
            if (!(message.Channel is SocketTextChannel guildChannel)) return;

            var parts = reaction.Emote.ToString()?.Split(':') ?? new string[0];

            var isSubscribing = reaction.Emote.Name == _configuration["UserUpdates:Reactions:Subscribe"];
            var isUnsubscribing = reaction.Emote.Name == _configuration["UserUpdates:Reactions:Unsubscribe"];

            // Check if emote means anything
            if (!isSubscribing && !isUnsubscribing) return;
            
            var roleName = _configuration["UserUpdates:RoleName"];
            
            IRole role = guildChannel.Guild.Roles.FirstOrDefault(x => x.Name == roleName);

            role ??= await guildChannel.Guild.CreateRoleAsync(roleName, GuildPermissions.None, isMentionable: false);

            if (role != null)
            {
                var user = guildChannel.Guild.GetUser(reaction.UserId);

                if (user != null)
                {
                    if (isSubscribing)
                    {
                        await user.AddRoleAsync(role);
                    }
                    else
                    {
                        await user.RemoveRoleAsync(role);
                    }
                }
            }

            await message.RemoveReactionAsync(reaction.Emote, reaction.UserId);
        }
    }
}
