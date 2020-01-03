
namespace Miki.Extensions.Localization
{
    using Miki.Localization;
    using Miki.Localization.Models;
    using System;

    public class HeaderResource : IResource
	{
		private readonly string _icon;
		private readonly IResource _resource;

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
