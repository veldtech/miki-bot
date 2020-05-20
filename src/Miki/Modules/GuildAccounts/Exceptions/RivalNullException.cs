using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Modules.GuildAccounts.Exceptions
{
    public class RivalNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_guild_rival_null");
    }
}
