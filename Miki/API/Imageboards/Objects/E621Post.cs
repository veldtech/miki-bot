using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;

namespace Miki.API.Imageboards.Objects
{
    internal class E621Post : BooruPost, ILinkable
    {
        public string Url => FileUrl;
		public string SourceUrl => Source;
		public string Provider => "E621";

		[JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("creator_id")]
        public string CreatorId { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("change")]
        public string Change { get; set; }

        [JsonProperty("created_at")]
        public Dictionary<string, string> CreatedAt { get; set; } = new Dictionary<string, string>();

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("fav_count")]
        public string FavouriteCount { get; set; }

        [JsonProperty("md5")]
        public string MD5 { get; set; }

        [JsonProperty("file_size")]
        public string FileSize { get; set; }

        [JsonProperty("file_url")]
        public string FileUrl { get; set; }

        [JsonProperty("file_ext")]
        public string FileExtension { get; set; }

        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty("preview_width")]
        public string PreviewWidth { get; set; }

        [JsonProperty("preview_height")]
        public string PreviewHeight { get; set; }

        [JsonProperty("sample_url")]
        public string SampleUrl { get; set; }

        [JsonProperty("sample_width")]
        public string SampleWidth { get; set; }

        [JsonProperty("sample_height")]
        public string SampleHeight { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("has_comments")]
        public string HasComments { get; set; }

        [JsonProperty("has_notes")]
        public string HasNotes { get; set; }

        [JsonProperty("has_children")]
        public string HasChildren { get; set; }

        [JsonProperty("children")]
        public string Children { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        [JsonProperty("artist")]
        public List<string> Artist { get; set; } = new List<string>();

        [JsonProperty("sources")]
        public List<string> Sources { get; set; } = new List<string>();
    }

}
