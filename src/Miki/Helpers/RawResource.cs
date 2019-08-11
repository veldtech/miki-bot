namespace Miki.Helpers
{
    using Miki.Localization;
    using System.Collections.Generic;
    using System.Linq;

    public class IconResource : IResource
	{
		private readonly string _icon;
		private readonly string _resourceText;

		public IconResource(string icon, string resource)
		{
			_icon = icon;
			_resourceText = resource;
		}

		public string Get(IResourceManager instance)
			=> _icon + " " + instance.GetString(_resourceText);
	}

	public class RawResource : IResource
	{
		private readonly string _content;

		public RawResource(string content)
		{
			_content = content;
		}
		public RawResource(IEnumerable<char> content)
			: this(new string(content.ToArray()))
		{ }

		public string Get(IResourceManager instance)
			 => _content;
	}
}