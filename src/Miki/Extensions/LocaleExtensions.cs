namespace Miki
{
    using System.Collections.Generic;
    using System.Resources;
    using Miki.Localization.Models;

    public static class LocaleExtensions
    {
        public static Locale DefaultLocale { get; set; }

        /// <summary>
        /// GetStringD or Default.
        /// </summary>
        public static string GetStringD(this Locale e, string key, params object[] args)
        {
            var value = e.GetString(key);
            if(string.IsNullOrWhiteSpace(value))
            {
                value = DefaultLocale.GetString(key);
            }

            return string.Format(value, args);
        }
    }

    public class LocaleResoureManager : IResourceManager
    {
        private readonly Dictionary<string, string> set;

        public LocaleResoureManager(Dictionary<string,string> set)
        {
            this.set = set;
        }

        /// <inheritdoc />
        public string GetString(string key)
        {
            if(set.TryGetValue(key, out var val))
            {
                return val;
            }
            return null;
        }
    }
}
