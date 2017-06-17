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
    public class RocketLeaguePlatform
    {
        [JsonProperty("id")]
        public int Id = 0;

        [JsonProperty("name")]
        public string Name = "";
    }
    internal class RocketLeaguePlatformCache : ICacheable<RocketLeaguePlatform>
    {
        string key = "";

        List<RocketLeaguePlatform> internalData = new List<RocketLeaguePlatform>();

        public List<RocketLeaguePlatform> Data
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

        public RocketLeaguePlatformCache(string k)
        {
            key = k;
            UpdateCache();
        }

        public async Task UpdateCache()
        {
            Data = new List<RocketLeaguePlatform>();
            RestClient rc = new RestClient("https://api.rocketleaguestats.com/v1/data/platforms")
                 .SetAuthorisation("Bearer", key);

            RestResponse<List<RocketLeaguePlatform>> cachedValues = await rc.GetAsync<List<RocketLeaguePlatform>>();
            LastUpdatedAt = DateTime.Now;
            foreach (RocketLeaguePlatform p in cachedValues.Data)
            {
                Data.Add(p);
            }
        }
    }
}
