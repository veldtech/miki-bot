namespace Miki.Modules.CustomCommands.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class CommandNullException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_command_null", commandName);

        private readonly string commandName;

        public CommandNullException(string commandName)
        {
            this.commandName = commandName;
        }
    }
}
