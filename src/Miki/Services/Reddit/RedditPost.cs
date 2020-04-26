namespace Miki.Services.Reddit
{
    using System.Runtime.Serialization;

    [DataContract]
    public class RedditPost
    {
        [DataMember(Name = "kind", Order = 1)]
        public string Kind { get; set; }

        [DataMember(Name = "data", Order = 2)]
        public RedditPostData Data { get; set; }
    }
}