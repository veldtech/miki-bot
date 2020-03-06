namespace Miki.Modules.CustomCommands
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class DuplicateComandException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_duplicate_command", $"`{commandName}`");

        private readonly string commandName;

        public DuplicateComandException(string commandName)
        {
            this.commandName = commandName;
        }
    }
}