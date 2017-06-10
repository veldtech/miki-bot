using Newtonsoft.Json;

namespace Miki.Extensions
{
    internal class OverwatchUserPlaytime
    {
        [JsonProperty("quick")]
        public string Quick { get; set; }

        [JsonProperty("competitive")]
        public string Competitive { get; set; }
    }
}