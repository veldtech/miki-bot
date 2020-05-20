using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Services.Pasta
{
    public class ActionUnauthorizedException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_action_unauthorized", action);
        private readonly string action;

        public ActionUnauthorizedException(string action)
        {
            this.action = action;
        }
    }
}