namespace Miki.Helpers
{
    using Miki.Localization.Models;

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
            => icon + "  " + instance.GetString(resourceText);
    }
}