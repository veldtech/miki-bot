using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Logging;
using Miki.Modules.CustomCommands.CommandHandlers;
using MiScript;
using MiScript.Models;
using MiScript.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.CustomCommands
{
    [Module("CustomCommands")]
    public class CustomCommandsModule
    {
        private Tokenizer _tokenizer = new Tokenizer();

        public CustomCommandsModule(Module mod, MikiApp app)
        {
            app.GetService<EventSystem>().AddCommandHandler(new CustomCommandsHandler());
        }

        [Command(Name = "createcommand", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task NewCustomCommandAsync(CommandContext e)
        {
            if(e.Arguments.Take(out string commandName))
            {
                if(commandName.Contains(' '))
                {
                    throw new InvalidCharacterException(" ");
                }

                if(!e.Arguments.CanTake)
                {
                    // TODO (Veld): Command has no function body.
                    return;
                }

                string scriptBody = e.Arguments.Pack.TakeAll().TrimStart('`').TrimEnd('`');

                try
                {
                    var tokens = _tokenizer.Tokenize(scriptBody);
                    new Parser(tokens).Parse(new Dictionary<string, object>());
                }
                catch(Exception ex)
                {
                    await e.ErrorEmbed($"An error occurred when parsing your script: ```{ex.ToString()}```")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return;
                }

                try
                {
                    var db = e.GetService<DbContext>();
                    await db.Set<CustomCommand>().AddAsync(new CustomCommand
                    {
                        CommandName = commandName.ToLowerInvariant(),
                        CommandBody = scriptBody,
                        GuildId = e.Guild.Id.ToDbLong()
                    });
                    await db.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Log.Error(ex);
                }

                await e.SuccessEmbed($"Created script '>{commandName}'")
                    .QueueToChannelAsync(e.Channel);
            }
        }
    }
}
