using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rest;
using IA.SDK;

namespace Miki.API.RocketLeague
{
    class RocketLeagueApi
    {
        string key = "";

        public ICacheable<RocketLeaguePlatform> platforms;
        public ICacheable<RocketLeaguePlaylist> playlists;
        public ICacheable<RocketLeagueSeason> seasons;
        public ICacheable<RocketLeagueTier> tiers;

        public RocketLeagueApi(RocketLeagueOptions o)
        {
            key = o.ApiKey;

            platforms = new RocketLeaguePlatformCache(key)
            {
                UpdateSpan = o.CacheTime,
            };
            playlists = new RocketLeaguePlaylistCache(key)
            {
                UpdateSpan = o.CacheTime,
            };
            seasons = new RocketLeagueSeasonCache(key)
            {
                UpdateSpan = o.CacheTime,
            };
            tiers = new RocketLeagueTierCache(key)
            {
                UpdateSpan = o.CacheTime,
            };
        }

        public async Task<RocketLeagueUser> GetUserAsync(string name, int platform = 1)
        {
            RestClient r = new RestClient($"https://api.rocketleaguestats.com/v1/player?unique_id={ name }&platform_id={ platform.ToString() }")
                .SetAuthorisation("Bearer", key);

            RestResponse <RocketLeagueUser> response = await r.GetAsync<RocketLeagueUser>();

            return response.Data;
        }

        public async Task<RocketLeagueSearchResult> SearchUsersAsync(string name, int page = 0, bool exact = false)
        {
            List<RocketLeagueUser> users = new List<RocketLeagueUser>();

            RestClient r = new RestClient($"https://api.rocketleaguestats.com/v1/search/players?display_name={ name }&page={ page }&exact={exact.ToString()}")
                .SetAuthorisation("Bearer", key);

            RestResponse<RocketLeagueSearchResult> response = await r.GetAsync<RocketLeagueSearchResult>();

            return response.Data;
        }
    }
}
