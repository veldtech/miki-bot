using Miki.Localization;
using Miki.Localization.Exceptions;
using System;
using System.Runtime.Serialization;

namespace Miki.Modules.CustomCommands
{
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