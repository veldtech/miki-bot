namespace Miki.Attributes
{
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using System.Threading.Tasks;
    using Miki.Framework.Extension;
    using Miki.Utility;

    public class GuildOnlyAttribute : CommandRequirementAttribute
    {
        public override Task<bool> CheckAsync(IContext e)
        {
            return Task.FromResult(e.GetGuild() != null);
        }

        public override async Task OnCheckFail(IContext e)
        {
            await e.ErrorEmbedResource("error_command_guildonly")
                .ToEmbed().QueueAsync(e, e.GetChannel() as IDiscordTextChannel);
        }
    }
}
