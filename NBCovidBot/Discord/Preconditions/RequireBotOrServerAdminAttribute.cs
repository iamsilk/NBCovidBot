using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace NBCovidBot.Discord.Preconditions
{
    public class RequireBotOrServerAdminAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var result = await new RequireBotAdminAttribute().CheckPermissionsAsync(context, command, services);
            
            if (result.IsSuccess) return result;

            return await new RequireUserPermissionAttribute(GuildPermission.Administrator).CheckPermissionsAsync(
                context,
                command, services);
        }
    }
}
