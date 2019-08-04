using Miki.Bot.Models;
using Miki.Localization.Exceptions;

namespace Miki.Exceptions
{
	public abstract class UserException : LocalizedException
	{
		protected readonly User _user;

		public UserException(User user) : base()
		{
			_user = user;
		}
	}
}