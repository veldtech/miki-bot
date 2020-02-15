namespace Miki.Services
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class GuildUserNullException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_guild_null");
    }
}