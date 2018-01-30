using Newtonsoft.Json;
using Rest;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.API.UrbanDictionary
{
    internal class UrbanDictionaryApi
    {
        private string key = "";

        public UrbanDictionaryApi(string key)
        {
            this.key = key;
        }

        public async Task<UrbanDictionaryEntry> GetEntryAsync(string term, int index = 0)
        {
            RestClient client = new RestClient("https://mashape-community-urban-dictionary.p.mashape.com/define?term=" + term);

            client.AddHeader("X-Mashape-Key", key);
            client.AddHeader("Accept", "application/json");

			RestResponse<UrbanDictionaryResponse> post = await client.GetAsync<UrbanDictionaryResponse>("");

            if (post.Data.Entries.Count == 0)
            {
                return null;
            }

			return post.Data.Entries.FirstOrDefault();
        }
    }
}