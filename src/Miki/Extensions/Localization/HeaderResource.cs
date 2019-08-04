using Miki.Localization;

namespace Miki.Extensions
{
    public class HeaderResource : IResource
    {
        private string _icon;
        private IResource _resource;

        public HeaderResource(string icon, IResource resource)
        {
            _icon = icon;
            _resource = resource;
        }
        public HeaderResource(string icon, string resourceName, params object[] @params)
            : this(icon, new LanguageResource(resourceName, @params))
        {
        }

        public string Get(IResourceManager instance)
        {
            return _icon + " " + _resource.Get(instance);
        }
    }
}
