using IA.SDK;
using Newtonsoft.Json;
using Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    public class RocketLeaguePlaylist
    {
        [JsonProperty("id")]
        public int Id { get; set; } = -1;

        [JsonProperty("platformId")]
        public int PlatformId { get; set; } = -1;

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("population")]
        public RocketLeaguePopulation Population { get; set; } = null;
    }
    public class RocketLeaguePopulation
    {
        [JsonProperty("players")]
        public int Players { get; set; } = 0;

        [JsonProperty("updatedAt")]
        internal ulong? UpdatedAt { get; set; }
    }
    internal class RocketLeaguePlaylistCache : ICacheable<RocketLeaguePlaylist>
    {
        string key = "";

        List<RocketLeaguePlaylist> internalData = new List<RocketLeaguePlaylist>();

        public List<RocketLeaguePlaylist> Data
        {
            get
            {
                if (LastUpdatedAt + UpdateSpan < DateTime.Now)
                {
                    UpdateCache();
                }
                return internalData;
            }
            private set
            {
                internalData = value;
            }
        }
        public DateTime LastUpdatedAt
        {
            get;
            set;
        }
        public TimeSpan UpdateSpan
        {
            get;
            set;
        }

        public RocketLeaguePlaylistCache(string k)
        {
            key = k;
            UpdateCache();
        }

        public async Task UpdateCache()
        {
            RestClient rc = new RestClient("https://api.rocketleaguestats.com/v1/data/playlists")
                 .SetAuthorisation("Bearer", key);

            RestResponse<List<RocketLeaguePlaylist>> cachedValues = await rc.GetAsync<List<RocketLeaguePlaylist>>();
            LastUpdatedAt = DateTime.Now;

            internalData = cachedValues.Data;
        }
    }
}
