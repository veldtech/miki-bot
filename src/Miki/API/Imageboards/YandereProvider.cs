using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API.Imageboards
{
    using System.Runtime.Serialization;
    using Miki.API.Imageboards.Objects;
    using Miki.Utility;
    using Newtonsoft.Json;
    
    [DataContract]
    public class YandereResponse
    {
        [DataMember(Name = "posts")]
        public List<YanderePost> Posts { get; set; }
    }
}
