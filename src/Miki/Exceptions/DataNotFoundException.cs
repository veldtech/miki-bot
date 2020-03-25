namespace Miki.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class DataNotFoundException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource 
            => new LanguageResource("error_data_not_found");
    }
}
