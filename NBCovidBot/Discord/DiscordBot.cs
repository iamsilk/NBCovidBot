using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBCovidBot.Commands;
using NBCovidBot.Discord.Announcements;
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
        private readonly AnnouncementsDbContext _dbContext;

        public DiscordBot(
            Runtime runtime,
            ILogger<DiscordBot> logger,
            IConfiguration configuration,
            CommandHandler commandHandler,
            DiscordSocketClient client,
            AnnouncementsDbContext dbContext)
        {
            _runtime = runtime;
            _logger = logger;
            _configuration = configuration;
            _commandHandler = commandHandler;
            _client = client;
            _dbContext = dbContext;
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

            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _dbContext.Database.MigrateAsync(cancellationToken);
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
    }
}
