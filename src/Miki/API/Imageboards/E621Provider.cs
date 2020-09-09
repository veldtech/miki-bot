using System.Collections.Generic;
using System.Runtime.Serialization;
using Miki.API.Imageboards.Objects;
using Newtonsoft.Json;

namespace Miki.API.Imageboards
{
    [DataContract]
    public class E621Response
    {
        [JsonProperty("posts")]
        public List<E621Post> Posts { get; set; }
    }
}
