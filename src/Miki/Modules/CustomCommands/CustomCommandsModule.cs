using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Miki.Attributes;
using Miki.Bot.Models;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Localization;
using Miki.Framework.Commands.Permissions.Attributes;
using Miki.Framework.Commands.Permissions.Models;
using Miki.Framework.Commands.Scopes.Attributes;
using Miki.Framework.Commands.Stages;
using Miki.Localization;
using Miki.Logging;
using Miki.Modules.CustomCommands.CommandHandlers;
using Miki.Modules.CustomCommands.Exceptions;
using Miki.Modules.CustomCommands.Providers;
using Miki.Modules.CustomCommands.Services;
using Miki.Modules.CustomCommands.Values;
using Miki.Utility;
using MiScript;
using MiScript.Analyzer;
using MiScript.Exceptions;
using Newtonsoft.Json;

namespace Miki.Modules.CustomCommands
{
    [Module("CustomCommands"), Emoji(AppProps.Emoji.Wrench)]
    public class CustomCommandsModule
    {
		public CustomCommandsModule(MikiApp app)
		{
            var pipeline = new CommandPipelineBuilder(app.Services)
                .UseStage(new CorePipelineStage())
                .UsePrefixes()
                .UseStage(new FetchDataStage())               
                .UseArgumentPack()
                .UseStage(new LocalizationPipelineStage(app.Services.GetService<ILocalizationService>()))
                .UseStage(new CustomCommandsHandler())
                .Build();
            app.Services.GetService<IDiscordClient>()
                .MessageCreate += async (e) => await pipeline.ExecuteAsync(e);
        }

        [Command("createcommand")]
        [GuildOnly]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task NewCustomCommandAsync(IContext e)
        {
            if(e.GetArgumentPack().Take(out string commandName))
            {
                if(commandName.Contains(' '))
                {
                    throw new InvalidCharacterException(" ");
                }

                var commandHandler = e.GetService<CommandTree>();
                if(commandHandler.GetCommand(commandName) != null)
                {
                    return;
                }

                if(!e.GetArgumentPack().CanTake)
                {
                    // TODO (Veld): Command has no function body.
                    return;
                }

                var scriptBody = e.GetArgumentPack().Pack.TakeAll().TrimStart('`').TrimEnd('`');
                var provider = new CodeProvider(scriptBody);
                var sb = new StringBuilder();
                var locale = e.GetLocale();
                
                sb.AppendLine(locale.GetString("customcommands_created", $">{commandName}"));
                
                var service = e.GetService<ICustomCommandsService>();
                
                try
                {
                    var global = await CustomCommandsService.CreateGlobalAsync(e);
                    var information = BlockInformation.Compile(scriptBody, global);

                    if (information.Messages.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine(e.GetLocale().GetString("customcommands_warnings").CapitalizeFirst().AsBold());
                        
                        foreach (var message in information.Messages)
                        {
                            if (message.Range.HasValue)
                            {
                                var range = message.Range.Value;
                                
                                sb.Append(locale.GetString("customcommands_line",
                                    range.StartLine,
                                    range.StartColumn + 1));
                                sb.Append(' ');
                            }
                            sb.AppendLine(message.Content);

                            if (message.Range.HasValue)
                            {
                                sb.AppendLine("```");
                                sb.AppendLine(scriptBody.GetPeek(message.Range.Value));
                                sb.AppendLine("```");
                            }
                        }
                    }
                }
                catch (MiScriptInvalidTokenException ex)
                {
                    await CustomCommandsService.SendErrorAsync(
                        e,
                        "error_miscript_parse",
                        ex.Message,
                        provider,
                        ex.Range);
                    return;
                }
                catch (MiScriptException ex)
                {
                    await CustomCommandsService.SendErrorAsync(
                        e,
                        "error_miscript_parse",
                        ex.Message,
                        provider,
                        ex.Position);
                    return;
                }
                catch (Exception ex)
                {
                    await CustomCommandsService.SendErrorAsync(
                        e, 
                        "error_miscript_parse",
                        "Internal error in MiScript: " + ex.Message,
                        provider);
                    return;
                }

				try
				{
                    var guildId = (long)e.GetGuild().Id;

                    await service.UpdateBodyAsync(guildId, commandName, scriptBody);
                }
				catch(Exception ex)
				{
					Log.Error(ex);
				}

                await e.SuccessEmbed(sb.ToString())
                    .QueueAsync(e, e.GetChannel());
            }
        }

        [Command("eval")]
        [GuildOnly]
        [RequiresScope("developer")]
        public async Task EvalAsync(IContext e)
        {
            if (!e.GetArgumentPack().CanTake)
            {
                return;
            }

            var scriptBody = e.GetArgumentPack().Pack.TakeAll().TrimStart('`').TrimEnd('`');
            var service = e.GetService<ICustomCommandsService>();

            await service.ExecuteCodeAsync(e, scriptBody);
        }

        [Command("removecommand")]
        [GuildOnly]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task RemoveCommandAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string commandName))
            {
                return;
            }

            var context = e.GetService<MikiDbContext>();
            var guildId = (long)e.GetGuild().Id;

            var cmd = await context.CustomCommands.FindAsync(guildId, commandName);
            if (cmd == null)
            {
                throw new CommandNullException(commandName);
            }

            context.Remove(cmd);
            await context.SaveChangesAsync();

            await e.SuccessEmbedResource("ok_command_deleted", commandName)
                .QueueAsync(e, e.GetChannel());
        }

        [Command("getcommand")]
        [GuildOnly]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task GetCommandAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string commandName))
            {
                return;
            }
            
            var service = e.GetService<ICustomCommandsService>();
            var guildId = (long)e.GetGuild().Id;
            var code = await service.GetBodyAsync(guildId, commandName);

            if (code == null)
            {
                return;
            }
            
            var message = e.GetLocale().GetString("customcommands_code", commandName);
            
            await new EmbedBuilder()
                .SetTitle(":gear:  Custom Commands")
                .SetDescription($"{message} ```lua\n{code}\n```")
                .SetColor(102, 117, 127)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("getcommandstorage")]
        [GuildOnly]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task GetCommandStorageAsync(IContext e)
        {
            var service = e.GetService<ICustomCommandsService>();
            var storage = await service.GetStorageAsync((long) e.GetGuild().Id);

            var sb = new StringBuilder();

            if (storage.Count > 0)
            {
                var limit = 1800 / storage.Count;

                sb.AppendLine("```json");
                foreach (var (key, value) in storage)
                {
                    var data = value.ToString(Formatting.None);

                    if (data.Length > limit)
                    {
                        data = data.Substring(0, limit - 3) + "...";
                    }

                    sb.Append(key);
                    sb.Append(": ");
                    sb.AppendLine(data);
                }

                sb.AppendLine("```");
            }

            var message = e.GetLocale().GetString("customcommands_storage");

            await new EmbedBuilder()
                .SetTitle(":gear:  Custom Commands")
                .SetDescription($"{message} {sb}")
                .SetColor(102, 117, 127)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [Command("getcommandinstructions")]
        [GuildOnly]
        [RequiresScope("developer")]
        public async Task GetInstructionsAsync(IContext e)
        {
            var service = e.GetService<ICustomCommandsService>();

            if (!e.GetArgumentPack().Take(out string commandName))
            {
                return;
            }

            var guildId = (long)e.GetGuild().Id;
            var block = await service.GetBlockAsync(guildId, commandName);

            if (!block.HasValue)
            {
                return;
            }

            var message = e.GetLocale().GetString("customcommands_instructions", commandName);

            await new EmbedBuilder()
                .SetTitle(":gear:  Custom Commands")
                .SetDescription($"{message} ```\n{block.Unwrap()}\n```")
                .SetColor(102, 117, 127)
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }
    }
}
