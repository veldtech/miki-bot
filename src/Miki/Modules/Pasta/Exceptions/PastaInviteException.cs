namespace Miki.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class PastaInviteException : LocalizedException
	{
		public override IResource LocaleResource => new LanguageResource("error_pasta_invite");
	}
}
