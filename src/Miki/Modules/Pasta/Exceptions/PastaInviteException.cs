using Miki.Localization;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
	public class PastaInviteException : LocalizedException
	{
		public override IResource LocaleResource => new LanguageResource("error_pasta_invite");
	}
}
