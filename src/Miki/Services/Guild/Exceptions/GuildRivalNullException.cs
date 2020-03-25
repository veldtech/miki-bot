namespace Miki.Services
{
    using System;
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class GuildRivalNullException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_guild_rival_null");

        public GuildRivalNullException()
        {
        }
        public GuildRivalNullException(Exception innerException)
            : base("Guild rival is null", innerException)
        {
        }
    }
}