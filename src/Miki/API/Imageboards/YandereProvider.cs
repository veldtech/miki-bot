using System.Collections.Generic;
using System.Runtime.Serialization;
using Miki.API.Imageboards.Objects;
using Newtonsoft.Json;

namespace Miki.API.Imageboards
{
    [DataContract]
    public class YandereResponse
    {
        [JsonProperty("posts")]
        public List<YanderePost> Posts { get; set; }
    }
}
