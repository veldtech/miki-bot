using System;
using System.Runtime.Serialization;
using Miki.Localization.Exceptions;
using Miki.Localization.Models;

namespace Miki.Services
{
    public class ActionUnauthorizedException : LocalizedException
    {
        public override IResource LocaleResource => new LanguageResource("error_action_unauthorized", action);
        private readonly string action;

        public ActionUnauthorizedException(string action)
        {
            this.action = action;
        }
    }
}