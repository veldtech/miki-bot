using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Miki.Modules.Fun.Exceptions;
using Newtonsoft.Json;

namespace Miki.Services.Reddit
{
    public class RedditService
    {
        private readonly HttpClient httpClient;
        private const string BaseUrl = "https://reddit.com";

        public RedditService()
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public string GetUrl(string path)
        {
            return BaseUrl + path;
        }

        public async Task<IEnumerable<RedditPostData>> GetPostsAsync(string subreddit, ListingType type)
        {
            var res = await httpClient.GetAsync(
                $"/r/{subreddit}/{type.ToString().ToLowerInvariant()}.json");
            if(!res.IsSuccessStatusCode)
            {
                throw new InternalServerErrorException("Reddit");
            }

            var content = await res.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<RedditResponse>(content);
            return parsed.Data.Children.Select(post => post.Data);
        }

        public async Task<RedditPostData> GetRandomPostAsync(string subreddit)
        {
            var res = await httpClient.GetAsync($"r/{subreddit}/random.json");
            if(!res.IsSuccessStatusCode)
            {
                throw new InternalServerErrorException("Reddit");
            }

            var content = await res.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<IReadOnlyList<RedditResponse>>(content);
            return parsed.First().Data.Children.First().Data;
        }
	}
}
