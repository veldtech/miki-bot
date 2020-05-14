using System.Collections.Generic;
using System.Runtime.Serialization;
using Miki.API.Imageboards.Objects;

namespace Miki.API.Imageboards
{
    [DataContract]
    public class YandereResponse
    {
        [DataMember(Name = "posts")]
        public List<YanderePost> Posts { get; set; }
    }
}
