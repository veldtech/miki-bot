using Newtonsoft.Json;
using System;

namespace Miki.Objects
{
    public class ImgurImage
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("datetime")]
        public int TimeCreatedInEpochTime;

        public DateTime TimeCreated => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(TimeCreatedInEpochTime);

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("animated")]
        public bool IsAnimated;

        [JsonProperty("width")]
        public int Width;

        [JsonProperty("height")]
        public int Height;

        [JsonProperty("size")]
        public int SizeInBytes;

        [JsonProperty("views")]
        public int Views;

        [JsonProperty("bandwidth")]
        public int BandWithConsumedInBytes;

        [JsonProperty("section")]
        public string Section;

        [JsonProperty("link")]
        public string Link;

        [JsonProperty("gifv")]
        public string LinkGifv;

        [JsonProperty("mp4")]
        public string LinkMp4;

        [JsonProperty("mp4_size")]
        public int Mp4SizeInBytes;

        [JsonProperty("looping")]
        public bool IsLooping;

        [JsonProperty("favorite")]
        public bool IsFavourited;

        [JsonProperty("nsfw")]
        public bool? IsNsfw;

        [JsonProperty("vote")]
        public string Vote;

        [JsonProperty("in_gallery")]
        public bool IsInGallery;
    }
}