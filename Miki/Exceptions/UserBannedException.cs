using Miki.Localization;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

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
