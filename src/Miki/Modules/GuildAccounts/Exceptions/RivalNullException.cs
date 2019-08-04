using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Modules.GuildAccounts.Exceptions
{
    public class RivalNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_guild_rival_null");
    }
}
