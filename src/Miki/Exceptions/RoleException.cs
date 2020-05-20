using Miki.Discord.Common;
using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki
{
    public class RoleException : LocalizedException
    {
        public override IResource LocaleResource => new LanguageResource("error_default");

        protected IDiscordRole role;

        public RoleException(IDiscordRole role)
        {
            this.role = role;
        }
    }
}
