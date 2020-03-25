namespace Miki.API.Imageboards.Objects
{
    using Miki.API.Imageboards.Interfaces;
    using Newtonsoft.Json;

    internal class SafebooruPost : BooruPost, ILinkable
	{
		public string Url =>
			$"https://safebooru.org/{((IsSample) ? "samples" : "images")}/{Directory}/{((IsSample) ? "sample_" : "")}{Image}";

		public string SourceUrl => $"https://safebooru.org/{ ((IsSample) ? "samples" : "images")}/{Directory}";
		public string Provider => "Safebooru";

		[JsonProperty("directory")]
		public string Directory { get; set; }

		[JsonProperty("hash")]
		public string Hash { get; set; }

		[JsonProperty("id")]
		public ulong Id { get; set; }

		[JsonProperty("image")]
		public string Image { get; set; }

		[JsonProperty("change")]
		public string Change { get; set; }

		[JsonProperty("owner")]
		public string Owner { get; set; }

		[JsonProperty("parent_id")]
		public ulong ParentId { get; set; }

		[JsonProperty("rating")]
		public string Rating { get; set; }

		[JsonProperty("sample")]
		public bool IsSample { get; set; }

		[JsonProperty("sample_height")]
		public int SampleHeight { get; set; }

		[JsonProperty("sample_width")]
		public int SampleWidth { get; set; }
	}
}