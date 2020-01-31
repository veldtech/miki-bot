namespace Miki.UrbanDictionary.Objects
{
    using System.Runtime.Serialization;

	[DataContract]
    public class UrbanDictionaryEntry
	{
        [DataMember(Name = "definition")]
		public string Definition { get; set; }

		[DataMember(Name = "permalink")]
		public string Permalink { get; set; }

		[DataMember(Name = "thumbs_up")]
		public int ThumbsUp { get; set; }

		[DataMember(Name = "thumbs_down")]
        public int ThumbsDown { get; set; }

		[DataMember(Name = "author")]
		public string Author { get; set; }

		[DataMember(Name = "word")]
        public string Term { get; set; }

		[DataMember(Name = "defid")]
		public string DefinitionId { get; set; }

		[DataMember(Name = "current_vote")]
		public string CurrentVote { get; set; }

		[DataMember(Name = "example")]
		public string Example { get; set; }

		public int Score => ThumbsUp - ThumbsDown;
    }
}