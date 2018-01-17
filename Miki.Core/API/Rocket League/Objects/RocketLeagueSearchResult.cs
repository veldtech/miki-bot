using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.RocketLeague
{
    internal class RocketLeagueSearchResult
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