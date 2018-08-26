using Newtonsoft.Json;
using Miki.Rest;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Miki.API.UrbanDictionary
{
	internal class UrbanDictionaryApi
	{
		RestClient client;

		private string key = "";

		public UrbanDictionaryApi(string key)
		{
			if(string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			this.key = key;

			client = new RestClient("https://mashape-community-urban-dictionary.p.mashape.com/")
				.AddHeader("X-Mashape-Key", key)
				.AddHeader("Accept", "application/json");
		}

		public async Task<UrbanDictionaryEntry> GetEntryAsync(string term, int index = 0)
		{
			RestResponse<UrbanDictionaryResponse> post = await client.GetAsync<UrbanDictionaryResponse>("define?term=" + term);

			if (post.Data.Entries.Count == 0)
			{
				return null;
			}

			return post.Data.Entries.OrderByDescending(x => x.ThumbsUp - x.ThumbsDown).FirstOrDefault();
		}
	}
}