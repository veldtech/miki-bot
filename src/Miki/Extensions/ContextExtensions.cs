
namespace Miki
{
    using System;
    using Miki.Framework;
    using Miki.Utility;
    using Sentry;
    using Splitio.Services.Client.Interfaces;

    public static class ContextExtensions
    {
        public static SentryEvent ToSentryEvent(this IContext e, Exception exception)
        {
            return new SentryEvent(exception)
            {
                User = new Sentry.Protocol.User
                {
                    Username = e.GetAuthor().GetFullName(),
                    Id = e.GetAuthor().Id.ToString()
                },
                Request = new Sentry.Protocol.Request
                {
                    QueryString = e.GetQuery(),
                    Url = e.Executable.ToString(),
                }
            };
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
