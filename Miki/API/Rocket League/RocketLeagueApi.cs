using Miki.Common;
using Miki.Rest;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    internal class RocketLeagueApi
    {
        private string key = "";

        public RocketLeagueApi(RocketLeagueOptions o)
        {
            key = o.ApiKey;
        }

        public async Task<RocketLeagueUser> GetUserAsync(string name, int platform = 1)
        {
            RestClient r = new RestClient($"https://api.rocketleaguestats.com/v1/player?unique_id={ name }&platform_id={ platform.ToString() }")
                .SetAuthorization("Bearer", key);

            RestResponse<RocketLeagueUser> response = await r.GetAsync<RocketLeagueUser>("");

            return response.Data;
        }

        public async Task<RocketLeagueSearchResult> SearchUsersAsync(string name, int page = 0, bool exact = false)
        {
            List<RocketLeagueUser> users = new List<RocketLeagueUser>();

            RestClient r = new RestClient($"https://api.rocketleaguestats.com/v1/search/players?display_name={ name }&page={ page }&exact={exact.ToString()}")
                .SetAuthorization("Bearer", key);

            RestResponse<RocketLeagueSearchResult> response = await r.GetAsync<RocketLeagueSearchResult>("");

            return response.Data;
        }
    }
}