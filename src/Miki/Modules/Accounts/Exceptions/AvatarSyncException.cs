using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Localization.Models;

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