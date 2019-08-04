using Miki.Bot.Models;
using Miki.Localization;

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