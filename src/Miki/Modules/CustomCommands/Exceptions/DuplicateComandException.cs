namespace Miki.Modules.CustomCommands
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class DuplicateComandException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_duplicate_command", $"`{_commandName}`");

        private string _commandName;

        public DuplicateComandException(string commandName)
        {
            _commandName = commandName;
        }
    }
}