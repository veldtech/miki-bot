using Miki.Localization.Exceptions;
using Miki.Models;

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