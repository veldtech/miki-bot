namespace Miki.Modules.Fun.Exceptions
{
    using System;
    using Imgur.API;
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class InternalServerErrorException : LocalizedException
    {
        private readonly string source;

        /// <inheritdoc />
        public override IResource LocaleResource
            => new LanguageResource("error_server_internal", source);

        public InternalServerErrorException(string source)
        {
            this.source = source;
        }
    }
}
