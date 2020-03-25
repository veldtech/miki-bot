namespace Miki.Modules.CustomCommands.Exceptions
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

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
