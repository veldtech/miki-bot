using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Miki.API.Imageboards.Objects
{
	internal class ImgurPost
	{
		[JsonProperty("data")]
		public List<ImgurImage> Entries { get; set; }

		[JsonProperty("success")]
		public string Success { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}

	public class ImgurImage
	{
		[JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
		public string Title { get; set; }

        [JsonProperty("description")]
		public string Description { get; set; }

        [JsonProperty("datetime")]
		public int TimeCreatedInEpochTime { get; set; }

        public DateTime TimeCreated => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(TimeCreatedInEpochTime);

		[JsonProperty("type")]
		public string Type { get; set; }

        [JsonProperty("animated")]
		public bool IsAnimated { get; set; }

        [JsonProperty("width")]
		public int Width { get; set; }

        [JsonProperty("height")]
		public int Height { get; set; }

        [JsonProperty("size")]
		public int SizeInBytes { get; set; }

        [JsonProperty("views")]
		public int Views { get; set; }

        [JsonProperty("bandwidth")]
		public int BandWithConsumedInBytes { get; set; }

        [JsonProperty("section")]
		public string Section { get; set; }

        [JsonProperty("link")]
		public string Link { get; set; }

        [JsonProperty("gifv")]
		public string LinkGifv { get; set; }

        [JsonProperty("mp4")]
		public string LinkMp4 { get; set; }

        [JsonProperty("mp4_size")]
		public int Mp4SizeInBytes { get; set; }

        [JsonProperty("looping")]
		public bool IsLooping { get; set; }

        [JsonProperty("favorite")]
		public bool IsFavourited { get; set; }

        [JsonProperty("nsfw")]
		public bool? IsNsfw { get; set; }

        [JsonProperty("vote")]
		public string Vote { get; set; }

        [JsonProperty("in_gallery")]
		public bool IsInGallery { get; set; }
    }
}