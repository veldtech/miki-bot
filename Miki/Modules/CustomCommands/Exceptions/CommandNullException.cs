using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Modules.CustomCommands.Exceptions
{
    public class CommandNullException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_command_null", _commandName);

        private string _commandName;

        public CommandNullException(string commandName)
        {
            _commandName = commandName;
        }
    }
}
