using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Services
{
    public class ReputationGiveEmptyException : LocalizedException
    {
        /// <inheritdoc />
        public override IResource LocaleResource => new LanguageResource("error_reputation_none");
    }
}