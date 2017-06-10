using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    class RocketLeagueSearchResult
    {
        [JsonProperty("page")]
        public string Page { get; set; }

        [JsonProperty("results")]
        public int Results { get; set; }

        [JsonProperty("totalResults")]
        public int TotalResults { get; set; }

        [JsonProperty("maxResultsPerPage")]
        public int MaxResultsPerPage { get; set; }

        [JsonProperty("data")]
        public List<RocketLeagueUser> Data { get; set; } = new List<RocketLeagueUser>();
    }
}
