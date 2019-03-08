using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Common.Packets;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Commands;
using MiScript;
using MiScript.Models;
using MiScript.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.CustomCommands.CommandHandlers
{
    public class CustomCommandsHandler : CommandHandler
    {
        const string CommandCacheKey = "customcommands";

        public Dictionary<string, object> CreateContext(CommandContext e)
        {
            var context = new Dictionary<string, object>
            {
                { "author", e.Author.Username + "#" + e.Author.Discriminator },
                { "author.id", e.Author.Id },
                { "author.bot", e.Author.IsBot },
                { "author.mention", e.Author.Mention },
                { "author.discrim", e.Author.Discriminator },
                { "author.name", e.Author.Username },
                { "channel", "#" + e.Channel.Name },
                { "channel.id", e.Channel.Id },
                { "channel.nsfw", e.Channel.IsNsfw },
                { "message", e.Message.Content },
                { "message.id", e.Message.Id }
            };

            int i = 0;
            if (e.Arguments != null)
            {
                while (e.Arguments.Take<string>(out var str))
                {
                    context.Add($"args.{i}", str);
                    i++;
                }
            }
            context.Add("args.count", i + 1);

            if (e.Guild != null)
            {
                context.Add("guild", e.Guild.Name);
                context.Add("guild.id", e.Guild.Id);
                context.Add("guild.owner.id", e.Guild.OwnerId);
                context.Add("guild.members", e.Guild.MemberCount);
                context.Add("guild.icon", e.Guild.IconUrl);
            }

            return context;
        }

        public override async Task CheckAsync(CommandContext message)
        {
            if(message.Message.Type != MessageType.GUILDTEXT)
            {
                return;
            }

            var channel = await message.Message.GetChannelAsync();
            var guild = await (channel as IDiscordGuildChannel).GetGuildAsync();

            var cache = message.GetService<IExtendedCacheClient>();
            IEnumerable<Token> tokens = null;

            string[] args = message.Message.Content.Substring(message.PrefixUsed.Length)
                .Split(' ');
            string commandName = args.FirstOrDefault()
                .ToLowerInvariant();

            

            var cachePackage = await cache.HashGetAsync<ScriptPackage>(CommandCacheKey, commandName + ":" + guild.Id);
            if (cachePackage != null)
            {
                tokens = ScriptPacker.Unpack(cachePackage);
            }
            else
            {
                var db = message.GetService<MikiDbContext>();
                var command = await db.CustomCommands
                    .FindAsync(guild.Id.ToDbLong(), commandName);

                if(command != null)
                {
                    tokens = new Tokenizer().Tokenize(command.CommandBody);
                }
            }

            if(tokens != null)
            {
                var context = CreateContext(message);
                message.Channel.QueueMessage(new Parser(tokens).Parse(context));
            }
        }
    }
}
