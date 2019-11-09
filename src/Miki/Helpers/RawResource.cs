namespace Miki.Helpers
{
    using Miki.Localization.Models;
    using System.Collections.Generic;
    using System.Linq;

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
            => icon + " " + instance.GetString(resourceText);
    }

    public class RawResource : IResource
	{
		private readonly string content;

        public RawResource(string content)
        {
            this.content = content;
        }
        public RawResource(IEnumerable<char> content)
            : this(new string(content.ToArray()))
        { }

        public string Get(IResourceManager instance)
             => content;
	}
}