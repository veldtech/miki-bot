namespace Miki.API.Imageboards
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    public class ImageboardConfigurations
	{
		public Uri QueryKey = null;

		public string ExplicitTag = "rating:explicit";
		public string QuestionableTag = "rating:questionable";
		public string SafeTag = "rating:safe";

		public bool NetUseCredentials = false;
		public ICredentials NetCredentials = CredentialCache.DefaultCredentials;
		public List<Tuple<string, string>> NetHeaders = new List<Tuple<string, string>>();
		public List<string> BlacklistedTags = new List<string>();
	}
}