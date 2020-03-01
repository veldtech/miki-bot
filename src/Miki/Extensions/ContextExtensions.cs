using System;
using System.Collections.Generic;
using System.Text;

namespace Miki
{
    using Miki.Framework;
    using Miki.Utility;
    using Sentry;

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
    }
}
