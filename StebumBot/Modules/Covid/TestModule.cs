using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using System.Threading.Tasks;

namespace StebumBot.Modules.Covid
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("test")]
        [Summary("A command for testing things")]
        public async Task TestAsync()
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder.AddField("Test Name", "Test Value")
                .WithAuthor(Context.Client.CurrentUser)
                .WithDescription("Test Description")
                .WithFooter("Test footer")
                .WithCurrentTimestamp()
                .WithTitle("Test Title")
                .WithUrl("https://experience.arcgis.com/experience/8eeb9a2052d641c996dba5de8f25a8aa");

            await ReplyAsync(embed: embedBuilder.Build());
        }
    }
}
