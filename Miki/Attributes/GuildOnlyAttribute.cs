using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Attributes
{
    public class GuildOnlyAttribute : CommandRequirementAttribute
    {
        public override ValueTask<bool> CheckAsync(IContext e)
        {
            return new ValueTask<bool>(e.GetGuild() != null);
        }

        public override async Task OnCheckFail(IContext e)
        {
            await e.ErrorEmbedResource("error_command_guildonly")
                .ToEmbed().QueueAsync(e.GetChannel() as IDiscordTextChannel);
        }
    }
}
