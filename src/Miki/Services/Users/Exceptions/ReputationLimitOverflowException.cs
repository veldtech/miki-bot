namespace Miki.Services
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    public class ReputationLimitOverflowException : LocalizedException
    {
        private readonly int userCount;
        private readonly int reputationCount;
        private readonly int reputationLeft;

        /// <inheritdoc />
        public override IResource LocaleResource
            => new LanguageResource(
                "error_reputation_limit", userCount, reputationCount, reputationLeft);

        public ReputationLimitOverflowException(
            int userCount, int reputationCount, int reputationLeft)
        {
            this.userCount = userCount;
            this.reputationCount = reputationCount;
            this.reputationLeft = reputationLeft;
        }
    }
}
