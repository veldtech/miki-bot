using Miki.Common;
using Newtonsoft.Json;
using Miki.Rest;
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

    internal class RocketLeagueTierCache
    {
        private string key = "";

        private List<RocketLeagueTier> internalData = new List<RocketLeagueTier>();

        public List<RocketLeagueTier> Data
        {
            get
            {
                if (LastUpdatedAt + UpdateSpan < DateTime.Now)
                {
                    UpdateCache().Wait();
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
            UpdateCache().Wait();
        }

        public async Task UpdateCache()
        {
            internalData = new List<RocketLeagueTier>();
            RestClient rc = new RestClient("https://api.rocketleaguestats.com/v1/data/tiers")
                 .SetAuthorization("Bearer", key);

            RestResponse<List<RocketLeagueTier>> cachedValues = await rc.GetAsync<List<RocketLeagueTier>>("");
            LastUpdatedAt = DateTime.Now;

            foreach (RocketLeagueTier p in cachedValues.Data)
            {
                Data.Add(p);
            }
        }
    }
}