using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Services.Marriages
{
    public class ProposalsEmptyException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_proposals_empty");
    }
}
