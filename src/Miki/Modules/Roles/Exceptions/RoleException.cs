using Miki.Discord.Common;
using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Modules.Roles.Exceptions
{
    public class RoleException : LocalizedException
    {
        public override IResource LocaleResource => new LanguageResource("error_default");

        protected IDiscordRole _role;

        public RoleException(IDiscordRole role)
        {
            _role = role;
        }
    }
}
