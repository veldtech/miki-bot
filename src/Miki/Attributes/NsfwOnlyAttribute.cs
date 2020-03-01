using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Attributes
{
    using System.Threading.Tasks;
    using Miki.Discord;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Utility;

    public class NsfwOnlyAttribute : CommandRequirementAttribute
    {
        /// <inheritdoc />
        public override Task<bool> CheckAsync(IContext e)
        {
            return Task.FromResult(e.GetChannel().IsNsfw);
        }

        /// <inheritdoc />
        public override Task OnCheckFail(IContext e)
        {
            return e.ErrorEmbed("This command can only be used in NSFW channels.")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }
    }
}
