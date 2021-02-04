#undef MIGRATION_GENERATION
// Uncomment definition when generating migrations
//#define MIGRATION_GENERATION

using Microsoft.EntityFrameworkCore;
using NBCovidBot.Covid.Models;

#if MIGRATION_GENERATION
using Microsoft.Extensions.Configuration;
#endif

namespace NBCovidBot.Covid
{
    public class CovidDataDbContext : DbContext
    {
        #if !MIGRATION_GENERATION
        private readonly Runtime _runtime;

        public CovidDataDbContext(Runtime runtime)
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ZoneDailyInfo>()
                .HasKey(x => new {x.ZoneNumber, x.LastUpdate});
        }

        public DbSet<ProvinceDailyInfo> ProvinceData { get; set; }

        public DbSet<ZoneDailyInfo> ZoneData { get; set; }
    }
}