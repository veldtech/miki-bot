using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
    public class AvatarSyncException : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_avatar_sync");

        public AvatarSyncException() : base()
        { }
    }
}