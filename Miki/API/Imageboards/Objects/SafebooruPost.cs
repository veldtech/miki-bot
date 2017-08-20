using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;

namespace Miki.API.Imageboards.Objects
{
    internal class SafebooruPost : BooruPost, ILinkable
    {
        public new string Url =>
            $"https://safebooru.org/{ ((Sample) ? "samples" : "images")}/{Directory}/{((Sample) ? "sample_" : "")}{Image}";

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
        public bool Sample { get; set; }

        [JsonProperty("sample_height")]
        public int SampleHeight { get; set; }

        [JsonProperty("sample_width")]
        public int SampleWidth { get; set; }
    }
}
