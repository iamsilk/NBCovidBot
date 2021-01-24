using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace NBCovidBot.Commands
{
    public class CommandHandler : IDisposable
    {
        private readonly ILogger<CommandHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        public CommandService Commands { get; }

        public CommandHandler(
            ILogger<CommandHandler> logger,
            IConfiguration configuration,
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider services)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _services = services;

            Commands = commands;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public void Dispose()
        {
            _client.MessageReceived -= HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage message)
        {
            _logger.LogTrace("Received message: " + message);

            if (!(message is SocketUserMessage userMessage)) return;

            var argPos = 0;

            var prefix = _configuration["commands:prefix"];

            if (string.IsNullOrWhiteSpace(prefix)) return;

            if (!userMessage.HasStringPrefix(prefix, ref argPos) || message.Author.IsBot)
                return;
            
            var context = new SocketCommandContext(_client, userMessage);

            _logger.LogInformation($"Executing command '{userMessage.Content}' from user {userMessage.Author}.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await Commands.ExecuteAsync(context, argPos, _services);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogDebug($"Successfully executed command {message.Content} in {stopwatch.ElapsedMilliseconds}");
            }
            else
            {
                _logger.LogWarning($"Error ({result.Error}) occurred while executing command {message.Content} - {result.ErrorReason}");
            }
        }
    }
}
