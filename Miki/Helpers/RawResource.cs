using Miki.Localization;

namespace Miki.Helpers
{
	internal class RawResource : IResource
	{
		private readonly string _content;

		public RawResource(string content)
		{
			_content = content;
		}

		public string Get(LocaleInstance instance)
		{
			return _content;
		}
	}
}