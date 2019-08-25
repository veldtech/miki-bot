using System;
using System.Collections.Generic;
using System.Text;
using Miki.Discord.Common.Utils;
using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Modules.Admin.Exceptions
{
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
