namespace Miki.UrbanDictionary
{
    using System.Collections.Generic;
    using Miki.Net.Http;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System.Web;
    using Miki.UrbanDictionary.Objects;

    public class UrbanDictionaryApi
	{
        private const string BaseUrl = "http://api.urbandictionary.com/v0/";
        private const string UserBaseUrl = "https://www.urbandictionary.com/";

        private readonly IHttpClient client;

        public UrbanDictionaryApi()
		{
			client = new HttpClient(BaseUrl)
				.AddHeader("Accept", "application/json");
        }

        /// <summary>
        /// Gets the front-end URL for the user based on the term.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public string GetUserDefinitionUrl(string term)
            => UserBaseUrl + "define.php?term=" + HttpUtility.UrlEncode(term);

        /// <summary>
        /// Get definitions based on Urban Dictionary terms.
        /// </summary>
        /// <param name="term">The term you are querying on urbandictionary</param>
		public async Task<IUrbanDictionaryResponse> SearchTermAsync(string term)
		{
            IHttpResponse post = await client.GetAsync("define?term=" + term);
            if(!post.Success)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<UrbanDictionaryResponse>(post.Body);
        }
	}
}