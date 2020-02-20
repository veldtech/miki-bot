namespace Miki
{
    using System.Collections.Generic;
    using System.Resources;
    using Miki.Framework.Commands.Localization;
    using Miki.Localization.Models;

    public static class LocaleExtensions
    {
        public static Locale DefaultLocale { get; set; }
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
            return LocaleExtensions.DefaultLocale.GetString(key);
        }
    }
}
