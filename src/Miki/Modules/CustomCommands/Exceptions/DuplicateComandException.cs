using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Modules.CustomCommands
{
    public class DuplicateCommandException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_duplicate_command", $"'{commandName}'");

        private readonly string commandName;

        public DuplicateCommandException(string commandName)
        {
            this.commandName = commandName;
        }
    }
}