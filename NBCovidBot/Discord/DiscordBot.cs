using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using NBCovidBot.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NBCovidBot.Discord
{
    public class DiscordBot : IHostedService
    {
        private readonly ILogger<DiscordBot> _logger;
        private readonly IConfiguration _configuration;
        private readonly CommandHandler _commandHandler;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public DiscordBot(
            ILogger<DiscordBot> logger,
            IConfiguration configuration,
            CommandHandler commandHandler,
            DiscordSocketClient client,
            IServiceProvider services)
        {
            _logger = logger;
            _configuration = configuration;
            _commandHandler = commandHandler;
            _client = client;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var token = _configuration["token"];

            if (string.IsNullOrWhiteSpace(token) || token == "CHANGEME")
            {
                _logger.LogCritical("A token must be specified in the config file.");

                Environment.Exit(-1);
                return;
            }

            _client.Log += OnLog;

            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, _configuration["token"]);
            await _client.StartAsync();

            var schedule = CrontabSchedule.TryParse(_configuration["schedules:covid"]);

            if (schedule == null)
            {
                _logger.LogWarning("Cron schedule for !covid is invalid. Schedule will not run.");
            }
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
