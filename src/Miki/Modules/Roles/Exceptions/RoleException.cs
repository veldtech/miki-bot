namespace Miki.Modules.Roles.Exceptions
{
    using Miki.Discord.Common;
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class RoleException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_default");

        protected IDiscordRole _role;

        public RoleException(IDiscordRole role)
        {
            _role = role;
        }
    }
}
