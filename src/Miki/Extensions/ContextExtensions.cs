using System;
using System.Collections.Generic;
using Miki.Discord.Rest.Exceptions;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Utility;
using Sentry;
using Splitio.Services.Client.Interfaces;

namespace Miki
{
    public static class ContextExtensions
    {
        public static SentryEvent ToSentryEvent(this Exception exception)
        {
            return new SentryEvent(exception)
            {
                Message = exception.ToString()
            };
        }
        public static SentryEvent ToSentryEvent(this IContext e, Exception exception)
        {
            var sentryEvent = exception.ToSentryEvent();

            sentryEvent.User = new Sentry.Protocol.User
            {
                Username = e.GetAuthor().GetFullName(),
                Id = e.GetAuthor().Id.ToString()
            };
            sentryEvent.Request = new Sentry.Protocol.Request
            {
                QueryString = e.GetQuery(),
                Url = e.Executable.ToString(),
            };
            
            sentryEvent.SetTag("locale", e.GetLocale().CountryCode ?? "eng");
            sentryEvent.SetTag("guild", e.GetGuild()?.Id.ToString() ?? "dm");
            return sentryEvent;
        }

        /// <summary>
        /// Checks a feature flag to see if the current guild is allowed to use this. Uses user ID as
        /// fallback for DM.
        /// </summary>
        /// <param name="e">Command context</param>
        /// <param name="featureName">split-io feature flag.</param>
        /// <returns></returns>
        public static bool HasFeatureEnabled(this IContext e, string featureName)
        {
            var split = e.GetService<ISplitClient>();
            var snowflake = e.GetGuild()?.Id ?? e.GetAuthor().Id;
            var treatment = split.GetTreatment(snowflake.ToString(), featureName);
            return treatment == "on";
        }
    }
}
