using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Functional;
using Miki.Modules.CustomCommands.Values;
using Miki.Services;
using Miki.Utility;
using MiScript;
using MiScript.Values;
using Newtonsoft.Json.Linq;

namespace Miki.Modules.CustomCommands.Services
{
    public class CustomCommandsService : ICustomCommandsService
    {
        private const string CommandCacheKey = "customcommands";

        /// <summary>
        /// Limit of the storage keys.
        /// </summary>
        private const int KeyLimit = 1;

        /// <summary>
        /// Limit of the storage keys when the guild owner is a donator.
        /// </summary>
        private const int DonatorKeyLimit = 25;
        
        /// <summary>
        /// Limit of the storage values in bytes.
        /// </summary>
        private const int ValueLimit = 4000;

        /// <summary>
        /// Default options.
        /// </summary>
        private static readonly ContextOptions DefaultOptions = new ContextOptions
        {
            InstructionLimit = 250,
            StackLimit = 10,
            ObjectItemLimit = 50,
            ArrayItemLimit = 50,
            StringLengthLimit = 250
        };

        /// <summary>
        /// Context options when the <see cref="IDiscordGuild.OwnerId"/> is a donator.
        /// </summary>
        private static readonly ContextOptions DonatorOptions = new ContextOptions
        {
            InstructionLimit = 5000,
            StackLimit = 20,
            ObjectItemLimit = 100,
            ArrayItemLimit = 100,
            StringLengthLimit = 500
        };

        private readonly IExtendedCacheClient cache;
        private readonly IUnitOfWork unitOfWork;
        private readonly IUserService userService;

        public CustomCommandsService(IExtendedCacheClient cache, IUnitOfWork unitOfWork, IUserService userService)
        {
            this.cache = cache;
            this.unitOfWork = unitOfWork;
            this.userService = userService;
        }

        /// <inheritdoc />
        public async ValueTask<Optional<Block>> GetBlockAsync(long guildId, string commandName)
        {
            var cachePackage = await cache.HashGetAsync<BlockCache>(
                CommandCacheKey, commandName + ":" + guildId);
            
            if (cachePackage != null)
            {
                await using var stream = new MemoryStream(cachePackage.Bytes);
                var reader = new BinaryReader(stream);

                return reader.ReadBlock();
            }

            var repository = unitOfWork.GetRepository<CustomCommand>();

            var command = await repository.GetAsync(guildId, commandName);

            if (command == null)
            {
                return Optional<Block>.None;
            }

            var block = BlockGenerator.Compile(command.CommandBody);

            await cache.HashUpsertAsync(
                CommandCacheKey, commandName + ":" + guildId, BlockCache.Create(block));

            return block;
        }

        /// <inheritdoc />
        public async ValueTask<string> GetBodyAsync(long guildId, string commandName)
        {
            var repository = unitOfWork.GetRepository<CustomCommand>();
            var command = await repository.GetAsync(guildId, commandName);

            return command?.CommandBody;
        }

        /// <inheritdoc />
        public async ValueTask UpdateBodyAsync(long guildId, string commandName, string scriptBody)
        {
            var repository = unitOfWork.GetRepository<CustomCommand>();
            var command = await repository.GetAsync(guildId, commandName);

            if (command == null)
            {
                await repository
                    .AddAsync(new CustomCommand
                    {
                        CommandName = commandName.ToLowerInvariant(),
                        CommandBody = scriptBody,
                        GuildId = guildId
                    })
                    .ConfigureAwait(false);
            }
            else
            {
                command.CommandBody = scriptBody;
                await repository.EditAsync(command).ConfigureAwait(false);
            }
                    
            await unitOfWork.CommitAsync().ConfigureAwait(false);
            await RemoveCacheAsync(guildId, commandName).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        public Task RemoveCacheAsync(long guildId, string commandName)
        {
            return cache.HashDeleteAsync(
                CommandCacheKey, commandName + ":" + guildId);
        }

        /// <inheritdoc />
        public async ValueTask<bool> ExecuteAsync(IContext e, string commandName)
        {
            var service = e.GetService<ICustomCommandsService>();
            var guild = e.GetGuild();
            var block = await service.GetBlockAsync((long) guild.Id, commandName);

            if (!block.HasValue)
            {
                return false;
            }
            
            var isDonator = await userService.UserIsDonatorAsync((long) guild.OwnerId);
            var options = isDonator ? DonatorOptions : DefaultOptions;
            var keyLimit = isDonator ? DonatorKeyLimit : KeyLimit;
            var storage = new ScriptStorage(cache, (long)e.GetGuild().Id, keyLimit, ValueLimit);

            var say = new ScriptSayFunction();
            var global = await CreateGlobalAsync(e, say);
            var runner = e.GetService<Runner>();
            var context = new Context(block, global, runner, options);

            global["storage"] = storage;
            
            await runner.ExecuteAsync(context);
            await SendResultAsync(e, say);
            await storage.UpdateAsync(context);
            
            return true;
        }

        /// <inheritdoc />
        public async ValueTask<IDictionary<string, JToken>> GetStorageAsync(long guildId)
        {
            var cacheKey = ScriptStorage.GetCacheKey(guildId);
            
            return (await cache.HashGetAllAsync<string>(cacheKey))
                .ToDictionary(
                    kv => kv.Key,
                    kv => JToken.Parse(kv.Value));
        }

        /// <summary>
        /// Create the global context for the runtime.
        /// </summary>
        private static async ValueTask<ScriptGlobal> CreateGlobalAsync(IContext e, IScriptValue say)
        {
            var context = new ScriptGlobal
            {
                ["author"] = ScriptValue.FromObject(new ScriptUser(e.GetAuthor())),
                ["channel"] = ScriptValue.FromObject(new ScriptChannel(e.GetChannel())),
                ["message"] = ScriptValue.FromObject(new ScriptMessage(e.GetMessage())),
                ["args"] = await CreateArgumentsAsync(e),
                ["say"] = say,
                ["embed"] = new CreateEmbedFunction()
            };

            if (e.GetGuild() != null)
            {
                context["guild"] = ScriptValue.FromObject(new ScriptGuild(e.GetGuild()));
            }

            return context;
        }

        /// <summary>
        /// Create arguments array.
        /// </summary>
        private static async ValueTask<ScriptArray> CreateArgumentsAsync(IContext e)
        {
            var args = new ScriptArray();
            var argumentPack = e.GetArgumentPack();

            if (argumentPack == null)
            {
                return args;
            }

            var guild = e.GetGuild();

            e.GetArgumentPack().Skip();

            while (argumentPack.Take<string>(out var str))
            {
                string value;

                if (Mention.TryParse(str, out var mention))
                {
                    value = mention.Type switch
                    {
                        MentionType.NONE => str,
                        MentionType.USER => (await guild.GetMemberAsync(mention.Id)).Username,
                        MentionType.USER_NICKNAME => (await guild.GetMemberAsync(mention.Id)).Username,
                        MentionType.ROLE => (await guild.GetRoleAsync(mention.Id)).Name,
                        MentionType.CHANNEL => '#' + (await guild.GetChannelAsync(mention.Id)).Name,
                        MentionType.EMOJI => str,
                        MentionType.ANIMATED_EMOJI => str,
                        MentionType.USER_ALL => "everyone",
                        MentionType.USER_ALL_ONLINE => "here",
                        _ => str
                    };
                }
                else
                {
                    value = str;
                }

                args.Add(ScriptValue.FromObject(value));
            }

            return args;
        }

        /// <summary>
        /// Send the result of <see cref="ScriptSayFunction"/>.
        /// </summary>
        private static async ValueTask SendResultAsync(IContext e, ScriptSayFunction say)
        {
            DiscordEmbed embed = null;

            if (say.EmbedBuilder != null)
            {
                var embedBuilder = say.EmbedBuilder.InnerEmbedBuilder;
                embed = embedBuilder.ToEmbed();

                // Don't send a empty embed to Discord.
                if (string.IsNullOrWhiteSpace(embed.Description) && string.IsNullOrWhiteSpace(embed.Title) &&
                    string.IsNullOrWhiteSpace(embed.Image?.Url) &&
                    (embed.Fields?.All(f => string.IsNullOrWhiteSpace(f.Content)) ?? true))
                {
                    embed = null;
                }
            }

            var output = say.Output;

            if (output.Length == 0 && embed == null)
            {
                return;
            }

            await e.GetChannel().SendMessageAsync(output.ToString(), false, embed);
        }
    }
}