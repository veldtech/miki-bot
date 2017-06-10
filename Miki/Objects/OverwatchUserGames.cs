using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.Objects
{
    internal class OverwatchUserGames
    {
        [JsonProperty("quick")]
        public List<int> Quick { get; set; } = new List<int>();

        [JsonProperty("competitive")]
        public List<int> Competitive { get; set; } = new List<int>();
    }
}