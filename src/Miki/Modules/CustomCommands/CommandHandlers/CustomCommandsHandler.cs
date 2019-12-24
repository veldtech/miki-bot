namespace Miki.Modules.CustomCommands.CommandHandlers
{
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Discord.Common;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Pipelines;
    using MiScript;
    using MiScript.Models;
    using MiScript.Parser;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Discord.Common.Packets.API;
    using Miki.Framework.Extension;

    public class CustomCommandsHandler : IPipelineStage
    {
        const string CommandCacheKey = "customcommands";

        public Dictionary<string, object> CreateContext(IContext e)
        {
            var context = new Dictionary<string, object>
            {
                { "author", e.GetAuthor().Username + "#" + e.GetAuthor().Discriminator },
                { "author.id", e.GetAuthor().Id },
                { "author.bot", e.GetAuthor().IsBot },
                { "author.mention", e.GetAuthor().Mention },
                { "author.discrim", e.GetAuthor().Discriminator },
                { "author.name", e.GetAuthor().Username },
                { "channel", "#" + e.GetChannel().Name },
                { "channel.id", e.GetChannel().Id },
                { "channel.nsfw", e.GetChannel().IsNsfw },
                { "message", e.GetMessage().Content },
                { "message.id", e.GetMessage().Id }
            };

            int i = 0;
            if (e.GetArgumentPack() != null)
            {
                while (e.GetArgumentPack().Take<string>(out var str))
                {
                    context.Add($"args.{i}", str);
                    i++;
                }
            }
            context.Add("args.count", i + 1);

            if (e.GetGuild() != null)
            {
                context.Add("guild", e.GetGuild().Name);
                context.Add("guild.id", e.GetGuild().Id);
                context.Add("guild.owner.id", e.GetGuild().OwnerId);
                context.Add("guild.members", e.GetGuild().MemberCount);
                context.Add("guild.icon", e.GetGuild().IconUrl);
            }

            return context;
        }

        public async ValueTask CheckAsync(IDiscordMessage data, IMutableContext e, Func<ValueTask> next)
        {
            if (e == null)
            {
                return;
            }   

            if (e.GetMessage().Type != DiscordMessageType.DEFAULT)
            {
                return;
            }

            var channel = e.GetChannel();
            if (!(channel is IDiscordGuildChannel guildChannel))
            {
                return;
            }

            var guild = await guildChannel.GetGuildAsync();

            var cache = e.GetService<IExtendedCacheClient>();
            IEnumerable<Token> tokens = null;

            string[] args = e.GetMessage().Content.Substring(e.GetPrefixMatch().Length)
                .Split(' ');
            string commandName = args.FirstOrDefault()
                .ToLowerInvariant();

            var cachePackage = await cache.HashGetAsync<ScriptPackage>(
                CommandCacheKey, 
                commandName + ":" + guild.Id);
            if (cachePackage != null)
            {
                tokens = ScriptPacker.Unpack(cachePackage);
            }
            else
            {
                var db = e.GetService<DbContext>();

                var command = await db.Set<CustomCommand>()
                    .FindAsync(guild.Id.ToDbLong(), commandName);
                if (command != null)
                {
                    tokens = new Tokenizer().Tokenize(command.CommandBody);
                }
            }

            if (tokens != null)
            {
                var context = CreateContext(e);
                e.GetChannel().QueueMessage(e, null, new Parser(tokens).Parse(context));
            }
        }
    }
}
