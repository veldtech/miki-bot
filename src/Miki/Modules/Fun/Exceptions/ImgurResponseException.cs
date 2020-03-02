namespace Miki.Modules.Fun.Exceptions
{
    using Imgur.API;
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class ImgurResponseException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource
            => new LanguageResource("error_server_internal", "imgur.com", Message);

        public ImgurResponseException(ImgurException ex)
            : base(ex.Message, ex)
        {
        }
    }
}
