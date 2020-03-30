namespace Miki.Services
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    public class ReputationGiveEmptyException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_reputation_none");
    }
}