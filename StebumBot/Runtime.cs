using Autofac.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using StebumBot.Commands;
using StebumBot.Discord;
using StebumBot.Scheduling;
using System;
using System.IO;
using System.Resources;
using System.Threading.Tasks;

namespace StebumBot
{
    public class Runtime
    {
        public IHost Host { get; private set; }

        private ILoggerFactory _loggerFactory;
        private ILogger<Runtime> _logger;

        public string WorkingDirectory { get; private set; }

        public Runtime()
        {
        }

        public async Task InitAsync()
        {
            var hostBuilder = new HostBuilder();

            WorkingDirectory = Environment.CurrentDirectory;

            SetupSerilog();

            hostBuilder
                .UseContentRoot(WorkingDirectory)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration(ConfigureConfiguration)
                .ConfigureServices(SetupServices)
                .UseSerilog();

            Host = hostBuilder.Build();

            await Host.StartAsync();
        }

        private void ExportResource(string resource)
        {
            var resourcePath = Path.Combine(WorkingDirectory, resource);

            if (!File.Exists(resourcePath))
            {
                using var stream = GetType().Assembly.GetManifestResourceStream("StebumBot." + resource);
                using var reader = new StreamReader(stream ?? throw new MissingManifestResourceException("Missing embedded resource"));

                var contents = reader.ReadToEnd();

                File.WriteAllText(resourcePath, contents);
            }
        }

        private void SetupSerilog()
        {
            const string configPath = "logging.yaml";

            ExportResource(configPath);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(WorkingDirectory)
                .AddYamlFile(configPath)
                .Build();

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

            var serilogLogger = Log.Logger = loggerConfiguration.CreateLogger();
            _loggerFactory = new SerilogLoggerFactory(serilogLogger);
            _logger = _loggerFactory.CreateLogger<Runtime>();
        }

        private void ConfigureConfiguration(IConfigurationBuilder builder)
        {
            const string configPath = "stebum.yaml";

            ExportResource(configPath);

            builder.SetBasePath(WorkingDirectory)
                .AddYamlFile(configPath, optional: false, reloadOnChange: true);
        }

        private void SetupServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            services.AddSingleton(this);
            services.AddSingleton<CommandHandler>();
            services.AddSingleton<DiscordSocketClient>();
            services.AddSingleton(typeof(IActionScheduler), typeof(ActionScheduler));
            services.AddTransient<CommandService>();
            services.AddHostedService<DiscordBot>();
        }
    }
}
