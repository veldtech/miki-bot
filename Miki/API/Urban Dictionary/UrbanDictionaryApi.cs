using Newtonsoft.Json;
using RestSharp;

namespace Miki.API.UrbanDictionary
{
    internal class UrbanDictionaryApi
    {
        private string key = "";

        public UrbanDictionaryApi(string key)
        {
            this.key = key;
        }

        public UrbanDictionaryEntry GetEntry(string term, int index = 0)
        {
            RestClient client = new RestClient("https://mashape-community-urban-dictionary.p.mashape.com/define?term=" + term);

            RestRequest r = new RestRequest();
            r.AddHeader("X-Mashape-Key", key);
            r.AddHeader("Accept", "application/json");

            RestResponse entry = (RestResponse)client.Execute(r);
            UrbanDictionaryResponse post = JsonConvert.DeserializeObject<UrbanDictionaryResponse>(entry.Content);

            if (post.Entries.Count == 0)
            {
                return null;
            }

            return (post.Entries.Count - 1 <= index) ? post.Entries[index] : post.Entries[post.Entries.Count - 1];
        }
    }
}