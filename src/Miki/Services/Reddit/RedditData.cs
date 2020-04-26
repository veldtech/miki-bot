namespace Miki.Services.Reddit
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class RedditData
    {
        [DataMember(Name = "modhash", Order = 1)]
        public string ModHash { get; set; }

        [DataMember(Name = "dist", Order = 2)]
        public int? Dist { get; set; }

        [DataMember(Name = "after", Order = 3)]
        public string After { get; set; }

        [DataMember(Name = "children", Order = 4)]
        public IReadOnlyList<RedditPost> Children { get; set; }
    }
}