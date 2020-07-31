using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands.Pipelines;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility
{
    public class MultiCommandHandler : IPipelineStage
    {
        private readonly IEnumerable<IPipelineStage> commandHandlers;

        public MultiCommandHandler(params IPipelineStage[] commandHandlers)
        {
            this.commandHandlers = commandHandlers;
        }

        public async ValueTask CheckAsync(IDiscordMessage data, IMutableContext e, [NotNull] Func<ValueTask> next)
        {
            if(e.Executable != null)
            {
                await next();
                return;
            }

            foreach(var handler in commandHandlers)
            {
                await handler.CheckAsync(data, e, () => default);
                if(e.Executable != null)
                {
                    await next();
                    return;
                }
            }
        }
    }
}
