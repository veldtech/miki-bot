namespace Miki.Modules.GuildAccounts.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class RivalNullException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_guild_rival_null");
    }
}
