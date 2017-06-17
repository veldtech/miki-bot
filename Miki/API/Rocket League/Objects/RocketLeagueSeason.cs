using IA.SDK;
using Newtonsoft.Json;
using Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    public class RocketLeagueSeason
    {
        [JsonProperty("seasonId")]
        public int Id;

        [JsonProperty("startedOn")]
        public ulong? StartedOn;

        [JsonProperty("endedOn")]
        public ulong? EndedOn;
    }
    internal class RocketLeagueSeasonCache : ICacheable<RocketLeagueSeason>
    {
        string key = "";

        List<RocketLeagueSeason> internalData = new List<RocketLeagueSeason>();

        public List<RocketLeagueSeason> Data
        {
            get
            {
                if (LastUpdatedAt + UpdateSpan < DateTime.Now)
                {
                    UpdateCache().GetAwaiter().GetResult();
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

        public RocketLeagueSeasonCache(string k)
        {
            key = k;
            UpdateCache();
        }

        public async Task UpdateCache()
        {
            internalData = new List<RocketLeagueSeason>();
            RestClient rc = new RestClient("https://api.rocketleaguestats.com/v1/data/seasons")
                 .SetAuthorisation("Bearer", key);

            RestResponse<List<RocketLeagueSeason>> cachedValues = await rc.GetAsync<List<RocketLeagueSeason>>();
            LastUpdatedAt = DateTime.Now;

            foreach (RocketLeagueSeason p in cachedValues.Data)
            {
                internalData.Add(p);
            }
        }
    }
}