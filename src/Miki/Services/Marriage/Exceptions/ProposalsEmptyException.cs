namespace Miki.Services.Marriage
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    public class ProposalsEmptyException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_proposals_empty");
    }
}
