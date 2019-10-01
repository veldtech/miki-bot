using Miki.Bot.Models;
using Miki.Localization;
using Miki.Localization.Models;
using Miki.Models;

namespace Miki.Exceptions
{
	public class UserBannedException : UserException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_user_banned");

		public UserBannedException(User user) : base(user)
		{
		}
	}
}