using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NBCovidBot.Discord.Preconditions
{
    public class RequireBotAdminAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            var admins = configuration.GetSection("Admins").Get<string[]>();

            return admins != null && admins.Contains(context.User.ToString())
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("User is not bot administrator"));
        }
    }
}
