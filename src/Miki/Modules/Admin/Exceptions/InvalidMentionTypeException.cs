
namespace Miki.Modules.Admin.Exceptions
{
    using Miki.Discord.Common;
    using Miki.Localization.Exceptions;
    using Miki.Localization;

    public class InvalidMentionTypeException : LocalizedException
    {
        public override IResource LocaleResource =>
            new LanguageResource("errors_mention_type_invalid",
                expectedType,
                actualType);

        private readonly MentionType expectedType;
        private readonly MentionType actualType;

        public InvalidMentionTypeException(MentionType expected, MentionType actual)
        {
            this.expectedType = expected;
            this.actualType = actual;
        }
    }
}
