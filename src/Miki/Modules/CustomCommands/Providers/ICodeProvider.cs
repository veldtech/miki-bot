using System.ComponentModel;
using System.Threading.Tasks;
using Miki.Cache;
using Miki.Modules.CustomCommands.Services;

namespace Miki.Modules.CustomCommands.Providers
{
    public interface ICodeProvider
    {
        ValueTask<string> GetAsync();
    }

    public class CodeProvider : ICodeProvider
    {
        private readonly string body;

        public CodeProvider(string body)
        {
            this.body = body;
        }

        public ValueTask<string> GetAsync()
        {
            return new ValueTask<string>(body);
        }
    }

    public class CommandBodyProvider : ICodeProvider
    {
        private readonly ICustomCommandsService service;
        private readonly long guildId;
        private readonly string commandName;

        public CommandBodyProvider(ICustomCommandsService service, long guildId, string commandName)
        {
            this.service = service;
            this.guildId = guildId;
            this.commandName = commandName;
        }

        public ValueTask<string> GetAsync()
        {
            return service.GetBodyAsync(guildId, commandName);
        }
    }
}