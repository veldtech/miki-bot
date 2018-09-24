using Miki.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.Helpers
{
	class RawResource : IResource
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
