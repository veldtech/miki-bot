using Miki.Framework.Exceptions;
using Miki.Localization.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
