using Miki.Functional;
using Miki.Localization;

namespace Miki.Utility
{
    public class FallbackResourceManager : IResourceManager
    {
        public static IResourceManager FallbackManager { get; set; }

        private readonly IResourceManager resource;

        public FallbackResourceManager(IResourceManager resource)
        {
            this.resource = resource;
        }

        /// <inheritdoc />
        public Optional<string> GetString(Required<string> key)
        {
            var value = resource.GetString(key);
            if(value.HasValue && !string.IsNullOrWhiteSpace(value.Unwrap()) 
               || FallbackManager == this)
            {
                return value.Unwrap();
            }
            return FallbackManager.GetString(key).Unwrap();
        }
    }
}
