using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki
{
    public class CommandDisabledException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_command_disabled");
    }
}
