using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;

namespace Miki.API.Imageboards.Objects
{
	internal class Rule34Post : BooruPost, ILinkable
	{
		public string Url => $"http://img.rule34.xxx/images/{Directory}/{Image}";
		public string SourceUrl => "";
		public string Provider => "Rule34";

		[JsonProperty("directory")]
		public string Directory { get; set; }

		[JsonProperty("hash")]
		public string Hash { get; set; }

		[JsonProperty("id")]
		public string Id { get; set; }

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
	}
}