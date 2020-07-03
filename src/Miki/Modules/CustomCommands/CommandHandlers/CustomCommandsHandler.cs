using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Pipelines;
using System;
using System.Threading.Tasks;
using Miki.Discord.Common.Packets.API;
using Miki.Modules.CustomCommands.Services;

namespace Miki.Modules.CustomCommands.CommandHandlers
{
    public class CustomCommandsHandler : IPipelineStage
    {
        public async ValueTask CheckAsync(IDiscordMessage data, IMutableContext e, Func<ValueTask> next)
        {
            if(e.Executable != null)
            {
                await next();
                return;
            }

            if (e == null)
            {
                return;
            }   

            if (e.GetMessage().Type != DiscordMessageType.DEFAULT)
            {
                await next();
                return;
            }

            var service = e.GetService<ICustomCommandsService>();
            var startIndex = e.GetPrefixMatch().Length;
            var message = e.GetMessage().Content;
            var endIndex = message.IndexOf(' ', startIndex);
            var commandName = endIndex == -1
                ? message.Substring(startIndex)
                : message.Substring(startIndex, endIndex - startIndex);

            if (!await service.ExecuteAsync(e, commandName))
            {
                await next();
            }
        }
    }
}
