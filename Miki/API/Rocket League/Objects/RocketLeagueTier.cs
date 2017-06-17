using IA.SDK;
using Newtonsoft.Json;
using Rest;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    public class RocketLeagueTier
    {
        [JsonProperty("tierId")]
        public int Id { get; set; } = -1;

        [JsonProperty("tierName")]
        public string Name { get; set; } = "";
    }
    internal class RocketLeagueTierCache : ICacheable<RocketLeagueTier>
    {
        string key = "";

        List<RocketLeagueTier> internalData = new List<RocketLeagueTier>();

        public List<RocketLeagueTier> Data
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

        public RocketLeagueTierCache(string k)
        {
            key = k;
            UpdateCache();
        }

        public async Task UpdateCache()
        {
            internalData = new List<RocketLeagueTier>();
            RestClient rc = new RestClient("https://api.rocketleaguestats.com/v1/data/tiers")
                 .SetAuthorisation("Bearer", key);

            RestResponse<List<RocketLeagueTier>> cachedValues = await rc.GetAsync<List<RocketLeagueTier>>();
            LastUpdatedAt = DateTime.Now;

            foreach (RocketLeagueTier p in cachedValues.Data)
            {
                Data.Add(p);
            }
        }
    }
}