using Miki.Localization;
using Miki.Localization.Exceptions;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Exceptions
{
	class InsufficientMarriageSlotsException : UserException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_marriageslots_insufficient", _user.Name);

		public InsufficientMarriageSlotsException(User user) : base(user)
		{ }
	}
}
