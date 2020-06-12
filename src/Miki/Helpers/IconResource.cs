using Miki.Localization;

namespace Miki.Helpers
{
    public class IconResource : IResource
    {
        private readonly string icon;
        private readonly string resourceText;

        public IconResource(string icon, string resource)
        {
            this.icon = icon;
            resourceText = resource;
        }

        public string Get(IResourceManager instance)
        {
            if (string.IsNullOrEmpty(icon))
            {
                return instance.GetString(resourceText).Unwrap();
            }
            return $"{icon}  {instance.GetString(resourceText).Unwrap()}";   
        }
    }
}