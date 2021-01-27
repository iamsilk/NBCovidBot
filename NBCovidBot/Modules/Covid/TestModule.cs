using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using System.Threading.Tasks;
// ReSharper disable PossibleMultipleEnumeration

namespace NBCovidBot.Modules.Covid
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class TestModule : ModuleBase<SocketCommandContext>
    {

        [Command("test")]
        [Summary("A command for testing things")]
        public async Task TestAsync()
        {
            var embedBuilder = new EmbedBuilder();
            
            embedBuilder
                .WithTitle("New Brunswick COVID-19 Statistics")
                .WithUrl("https://experience.arcgis.com/experience/8eeb9a2052d641c996dba5de8f25a8aa")
                .AddField("Total", "Example", true)
                .AddField("Fredericton", "Example", true)
                .AddField("Moncton", "Example", true)
                .WithCurrentTimestamp();

            await ReplyAsync(embed: embedBuilder.Build());
        }
    }
}
