// Uncomment definition when generating migrations
#define MIGRATION_GENERATION

using Microsoft.EntityFrameworkCore;
using NBCovidBot.Discord.Announcements.Models;
#if MIGRATION_GENERATION
using Microsoft.Extensions.Configuration;
#endif

namespace NBCovidBot.Discord.Announcements
{
    public class AnnouncementsDbContext : DbContext
    {
#if !MIGRATION_GENERATION
        private readonly Runtime _runtime;

        public AnnouncementsDbContext(Runtime runtime)
        {
            _runtime = runtime;
        }
#endif

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if MIGRATION_GENERATION
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.Environment.CurrentDirectory)
                .AddYamlFile("config.yaml")
                .Build();
#else
            var configuration = _runtime.Configuration;
#endif

            optionsBuilder.UseMySql(configuration["Database:ConnectionStrings:Default"]);
        }

        public DbSet<Announcement> Announcements { get; set; }
    }
}
