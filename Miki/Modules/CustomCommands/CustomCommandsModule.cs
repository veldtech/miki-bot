using Microsoft.EntityFrameworkCore;
using Miki.Attributes;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Logging;
using Miki.Modules.CustomCommands.CommandHandlers;
using Miki.Modules.CustomCommands.Exceptions;
using MiScript;
using MiScript.Models;
using MiScript.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        [GuildOnly, Command(Name = "createcommand", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task NewCustomCommandAsync(ICommandContext e)
        {
            if(e.Arguments.Take(out string commandName))
            {
                if(commandName.Contains(' '))
                {
                    throw new InvalidCharacterException(" ");
                }

                if(e.EventSystem.GetCommandHandler<SimpleCommandHandler>()
                    .GetCommandByIdOrDefault(commandName) != null)
                {
                    throw new DuplicateComandException(commandName);
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
                    var values = tokens.Where(x => x.TokenType == Tokens.Argument)
                        .Select(x => x.Value);

                    var context = new Dictionary<string, object>();
                    foreach(var v in values)
                    {
                        if(context.ContainsKey(v))
                        {
                            continue;
                        }
                        context.Add(v, "");
                    }

                    new Parser(tokens).Parse(context);
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

        [GuildOnly, Command(Name = "removecommand", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task RemoveCommandAsync(ICommandContext e)
        {
            var context = e.GetService<MikiDbContext>();
            var guildId = e.Guild.Id.ToDbLong();

            if (e.Arguments.Take(out string commandName))
            {
                var cmd = await context.CustomCommands.FindAsync(guildId, commandName);
                if(cmd == null)
                {
                    throw new CommandNullException(commandName);
                }

                context.Remove(cmd);
                await context.SaveChangesAsync();

                await e.SuccessEmbedResource("ok_command_deleted", commandName)
                    .QueueToChannelAsync(e.Channel);
            }
            else
            {
                // No arguments provided
            }
        }
    }
}
