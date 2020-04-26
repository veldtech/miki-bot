namespace Miki.Services.Reddit
{
    using System.Runtime.Serialization;
    
    [DataContract]
    public class RedditResponse
    {
        [DataMember(Name = "kind", Order = 1)]
        public string Kind { get; set; }

        [DataMember(Name = "data", Order = 2)]
        public RedditData Data { get; set; }
    }
}