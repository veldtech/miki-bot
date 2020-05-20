using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Services
{
    public class GuildUserNullException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_guild_null");
    }
}