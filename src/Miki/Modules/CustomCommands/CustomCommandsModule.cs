namespace Miki.Modules.CustomCommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.EntityFrameworkCore;
    using Miki.Attributes;
    using Miki.Bot.Models;
    using Miki.Discord;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Attributes;
    using Miki.Framework.Commands.Stages;
    using Miki.Framework.Events;
    using Miki.Framework.Events.Triggers;
    using Miki.Logging;
    using Miki.Modules.CustomCommands.CommandHandlers;
    using Miki.Modules.CustomCommands.Exceptions;
    using MiScript;
    using MiScript.Models;
    using MiScript.Parser;

    [Module("CustomCommands")]
    public class CustomCommandsModule
    {
        private readonly Tokenizer tokenizer = new Tokenizer();

		public CustomCommandsModule(MikiApp app)
		{
            var pipeline = new CommandPipelineBuilder(app.Services)
                .UseStage(new CorePipelineStage())
                .UsePrefixes(
                    new PrefixTrigger(">", true, true),
                    new PrefixTrigger("miki.", true),
                    new MentionTrigger())
                .UseStage(new FetchDataStage())               
                .UseArgumentPack()
                .UseStage(new CustomCommandsHandler())
                .Build();
			//app.Services.GetService<IDiscordClient>()
			//	.MessageCreate += async (e) => await pipeline.ExecuteAsync(e);
		}

        [GuildOnly, Command("createcommand")]
        public async Task NewCustomCommandAsync(IContext e)
        {
            if(e.GetArgumentPack().Take(out string commandName))
            {
                if(commandName.Contains(' '))
                {
                    throw new InvalidCharacterException(" ");
                }

                var commandHandler = e.GetStage<CommandHandlerStage>();
                if(commandHandler.GetCommand(commandName) != null)
                {
                    return;
                }

                if(!e.GetArgumentPack().CanTake)
                {
                    // TODO (Veld): Command has no function body.
                    return;
                }

                string scriptBody = e.GetArgumentPack().Pack.TakeAll().TrimStart('`').TrimEnd('`');

                try
                {
                    var tokens = tokenizer.Tokenize(scriptBody).ToList();
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
                    await e.ErrorEmbed($"An error occurred when parsing your script: ```{ex}```")
                        .ToEmbed().QueueAsync(e.GetChannel());
                    return;
                }

				try
				{
					var db = e.GetService<DbContext>();
					await db.Set<CustomCommand>()
                        .AddAsync(new CustomCommand
					{
						CommandName = commandName.ToLowerInvariant(),
						CommandBody = scriptBody,
						GuildId = e.GetGuild().Id.ToDbLong()
					}).ConfigureAwait(false);
					await db.SaveChangesAsync()
                        .ConfigureAwait(false);
				}
				catch(Exception ex)
				{
					Log.Error(ex);
				}

                await e.SuccessEmbed($"Created script '>{commandName}'")
                    .QueueAsync(e.GetChannel());
            }
        }

        [GuildOnly, Command("removecommand")]
        public async Task RemoveCommandAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();
            var guildId = e.GetGuild().Id.ToDbLong();

            if (e.GetArgumentPack().Take(out string commandName))
            {
                var cmd = await context.CustomCommands.FindAsync(guildId, commandName);
                if(cmd == null)
                {
                    throw new CommandNullException(commandName);
                }

                context.Remove(cmd);
                await context.SaveChangesAsync();

                await e.SuccessEmbedResource("ok_command_deleted", commandName)
                    .QueueAsync(e.GetChannel());
            }
            else
            {
                // No arguments provided
            }
        }
    }
}
