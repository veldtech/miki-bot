using Miki.Localization;
using Newtonsoft.Json;
using Miki.Functional;

namespace Miki.Extensions
{
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
