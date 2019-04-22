using Miki.Discord;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Attributes
{
    public class GuildOnlyAttribute : CommandRequirementAttribute
    {
        public override Task<bool> CheckAsync(ICommandContext e)
        {
            return Task.FromResult(e.Guild != null);
        }

        public override async Task OnCheckFail(ICommandContext e)
        {
            await e.ErrorEmbedResource("error_command_guildonly")
                .ToEmbed().QueueToChannelAsync(e.Channel);
        }
    }
}
