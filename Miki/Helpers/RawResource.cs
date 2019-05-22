using Miki.Localization;

namespace Miki.Helpers
{
    public class IconResource : IResource
    {
        private string _icon;
        private string _resourceText;

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

		public string Get(IResourceManager instance)
		{
			return _content;
		}
	}
}