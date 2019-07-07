using Miki.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web;

namespace Miki.UrbanDictionary
{
    public class UrbanDictionaryAPI
	{
        private const string BaseURL = "http://api.urbandictionary.com/v0/";
        private const string UserBaseURL = "https://www.urbandictionary.com/";

        private readonly HttpClient client;
   
		public UrbanDictionaryAPI()
		{
			client = new HttpClient(BaseURL)
				.AddHeader("Accept", "application/json");
		}

        public string GetUserDefinitionURL(string term)
            => UserBaseURL + "define.php?term=" + HttpUtility.UrlEncode(term);

        /// <summary>
        /// Get definitions based on Urban Dictionary terms.
        /// </summary>
        /// <param name="term">The term you are querying on urbandictionary</param>
		public async Task<UrbanDictionaryResponse> SearchTermAsync(string term)
		{
            HttpResponse post = await client.GetAsync("/define?term=" + term);
            if (post.Success)
            {
                return JsonConvert.DeserializeObject<UrbanDictionaryResponse>(post.Body);
            }
            return null;
		}
	}
}