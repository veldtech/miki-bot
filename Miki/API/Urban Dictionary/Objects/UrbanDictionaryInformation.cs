using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.UrbanDictionary
{
    internal class UrbanDictionaryResponse
    {
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("result_type")]
        public string ResultType { get; set; }

        [JsonProperty("list")]
        public List<UrbanDictionaryEntry> Entries { get; set; }

        [JsonProperty("sounds")]
        public List<string> Sounds { get; set; }
    }
}