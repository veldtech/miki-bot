using Miki.Localization;
using Newtonsoft.Json;

namespace Miki.Extensions
{
    using Functional;

    public static class LocaleExtensions
    {
        public static string GetString(
            this IResourceManager manager,
            Required<string> key,
            params object[] args)
        {
            return string.Format(manager.GetString(key), args);
        }
    }
}
