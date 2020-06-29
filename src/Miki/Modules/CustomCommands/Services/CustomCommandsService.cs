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

namespace Miki.Modules.CustomCommands.Services
{
    public class CustomCommandsService : ICustomCommandsService
    {
        private const string CommandCacheKey = "customcommands";
        
        /// <summary>
        /// Default options.
        /// </summary>
        private static readonly ContextOptions DefaultOptions = new ContextOptions
        {
            InstructionLimit = 250,
            StackLimit = 10
        };

        /// <summary>
        /// Context options when the <see cref="IDiscordGuild.OwnerId"/> is a donator.
        /// </summary>
        private static readonly ContextOptions DonatorOptions = new ContextOptions
        {
            InstructionLimit = 5000,
            StackLimit = 20
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
            Block block;

            var cachePackage = await cache.HashGetAsync<BlockCache>(
                CommandCacheKey, commandName + ":" + guildId);
            
            if (cachePackage != null)
            {
                await using var stream = new MemoryStream(cachePackage.Bytes);
                var reader = new BinaryReader(stream);

                block = reader.ReadBlock();
            }
            else
            {
                var repository = unitOfWork.GetRepository<CustomCommand>();

                var command = await repository.GetAsync(guildId, commandName);

                if (command == null)
                {
                    return Optional<Block>.None;
                }

                block = BlockGenerator.Compile(command.CommandBody);

                await using var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);

                writer.WriteBlock(block);
                writer.Flush();

                await cache.HashUpsertAsync(
                    CommandCacheKey, commandName + ":" + guildId, BlockCache.Create(block));
            }

            return block;
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
        public async ValueTask<ContextOptions> GetOptionsAsync(IDiscordGuild guild)
        {
            var isDonator = await userService.UserIsDonatorAsync((long) guild.OwnerId);

            return isDonator ? DonatorOptions : DefaultOptions;
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

            var say = new ScriptSayFunction();
            var global = CreateGlobal(e, say);
            var runner = e.GetService<Runner>();
            var options = await service.GetOptionsAsync(guild);
            
            await runner.ExecuteAsync(block, global, options);
            await SendResultAsync(e, say);
            
            return true;
        }
        
        /// <summary>
        /// Create the global context for the runtime.
        /// </summary>
        private static ScriptGlobal CreateGlobal(IContext e, IScriptValue say)
        {
            var args = new ScriptArray();
            
            if (e.GetArgumentPack() != null)
            {
                e.GetArgumentPack().Skip();
                while (e.GetArgumentPack().Take<string>(out var str))
                {
                    args.Add(ScriptValue.FromObject(str));
                }
            }

            var context = new ScriptGlobal
            {
                ["author"] = ScriptValue.FromObject(new ScriptUser(e.GetAuthor())),
                ["channel"] = ScriptValue.FromObject(new ScriptChannel(e.GetChannel())),
                ["message"] = ScriptValue.FromObject(new ScriptMessage(e.GetMessage())),
                ["args"] = args,
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