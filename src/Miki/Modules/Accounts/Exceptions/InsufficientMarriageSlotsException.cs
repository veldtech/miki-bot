using Miki.Bot.Models.Exceptions;
using Miki.Bot.Models;
using Miki.Localization;

namespace Miki.Exceptions
{
	class InsufficientMarriageSlotsException : UserException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_marriageslots_insufficient", User.Name);

		public InsufficientMarriageSlotsException(User user) : base(user)
		{ }
	}
}
