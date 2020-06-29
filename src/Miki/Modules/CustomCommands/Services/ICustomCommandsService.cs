using System.Threading.Tasks;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Functional;
using MiScript;

namespace Miki.Modules.CustomCommands.Services
{
    public interface ICustomCommandsService
    {
        /// <summary>
        /// Get the block for the <see cref="commandName"/>.
        /// </summary>
        ValueTask<Optional<Block>> GetBlockAsync(long guildId, string commandName);

        /// <summary>
        /// Update the code for the <see cref="commandName"/>.
        /// </summary>
        ValueTask UpdateBodyAsync(long guildId, string commandName, string scriptBody);

        /// <summary>
        /// Remove the <see cref="Block"/> cache for the <see cref="commandName"/>.
        /// </summary>
        Task RemoveCacheAsync(long guildId, string commandName);

        /// <summary>
        /// Execute the custom command.
        /// </summary>
        ValueTask<bool> ExecuteAsync(IContext e, string commandName);
    }
}