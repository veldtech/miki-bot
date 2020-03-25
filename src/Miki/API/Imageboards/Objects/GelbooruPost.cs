namespace Miki.API.Imageboards.Objects
{
    using Miki.API.Imageboards.Interfaces;
    using Newtonsoft.Json;

    internal class GelbooruPost : BooruPost, ILinkable
	{
		public string Url => FileUrl;
		public string SourceUrl => "";
		public string Provider => "Gelbooru";

		[JsonProperty("directory")]
		public string Directory { get; set; }

		[JsonProperty("hash")]
		public string Hash { get; set; }

		[JsonProperty("image")]
		public string Image { get; set; }

		[JsonProperty("change")]
		public string Change { get; set; }

		[JsonProperty("owner")]
		public string Owner { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonProperty("rating")]
		public string Rating { get; set; }

		[JsonProperty("sample")]
		public string Sample { get; set; }

		[JsonProperty("sample_height")]
		public string SampleHeight { get; set; }

		[JsonProperty("sample_width")]
		public string SampleWidth { get; set; }

		[JsonProperty("file_url")]
		public string FileUrl { get; set; }
	}
}