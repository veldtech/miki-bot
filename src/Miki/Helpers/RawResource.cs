using System.Collections.Generic;
using System.Linq;
using Miki.Localization;

namespace Miki.Helpers
{
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