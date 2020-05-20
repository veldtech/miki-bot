using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Exceptions
{
    public class PastaInviteException : LocalizedException
	{
		public override IResource LocaleResource => new LanguageResource("error_pasta_invite");
	}
}
