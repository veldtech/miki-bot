using Miki.Functional;
using Miki.Localization;

namespace Miki
{
    public static class LocaleExtensions
    {
        public static Optional<string> GetString(
            this IResourceManager manager,
            Required<string> key,
            params object[] args)
        {
            var str = manager.GetString(key);
            if (!str.HasValue)
            {
                return Optional<string>.None;
            }
            return string.Format(str.Unwrap(), args);
        }
    }
}
