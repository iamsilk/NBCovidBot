using Autofac.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBCovidBot.Commands;
using NBCovidBot.Covid;
using NBCovidBot.Discord;
using NBCovidBot.Discord.Announcements;
using NBCovidBot.Scheduling;
using Serilog;
using System;
using System.IO;
using System.Resources;
using System.Threading.Tasks;

namespace NBCovidBot
{
    public class Runtime
    {
        public IHost Host { get; private set; }

        public IConfiguration Configuration { get; private set; }

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

            using (Host = hostBuilder.Build())
            {
                await Host.RunAsync();
            }
        }

        private void ExportResource(string resource)
        {
            var resourcePath = Path.Combine(WorkingDirectory, resource);

            if (File.Exists(resourcePath)) return;

            using var stream = GetType().Assembly.GetManifestResourceStream("NBCovidBot." + resource);
            using var reader = new StreamReader(stream ?? throw new MissingManifestResourceException("Missing embedded resource"));

            var contents = reader.ReadToEnd();

            File.WriteAllText(resourcePath, contents);
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

            Log.Logger = loggerConfiguration.CreateLogger();
        }

        private void ConfigureConfiguration(IConfigurationBuilder builder)
        {
            const string configPath = "config.yaml";

            ExportResource(configPath);

            builder.SetBasePath(WorkingDirectory)
                .AddYamlFile(configPath, optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }

        private void SetupServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true))
                .AddSingleton(this)
                .AddSingleton<CommandHandler>()
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig()
                {
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton<ActionScheduler>()
                .AddSingleton<CovidDataProvider>()
                .AddSingleton<CovidDataFormatter>()
                .AddSingleton<CovidAnnouncer>()
                .AddTransient<CommandService>()
                .AddDbContext<AnnouncementsDbContext>()
                .AddHostedService<DiscordBot>();
        }
    }
}
