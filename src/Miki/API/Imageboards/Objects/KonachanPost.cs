using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.Imageboards.Objects
{
    internal class KonachanPost : BooruPost, ILinkable
    {
        public string Url => "http:" + FileUrl;
        public string SourceUrl => Source;
        public string Provider => "Konachan";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("creator_id")]
        public string CreatorId { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("change")]
        public string Change { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("md5")]
        public string MD5 { get; set; }

        [JsonProperty("file_size")]
        public string FileSize { get; set; }

        [JsonProperty("file_url")]
        public string FileUrl { get; set; }

        [JsonProperty("is_shown_in_index")]
        public string IsShownInIndex { get; set; }

        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty("preview_width")]
        public string PreviewWidth { get; set; }

        [JsonProperty("preview_height")]
        public string PreviewHeight { get; set; }

        [JsonProperty("actual_preview_width")]
        public string ActualPreviewWidth { get; set; }

        [JsonProperty("actual_preview_height")]
        public string ActualPreviewHeight { get; set; }

        [JsonProperty("sample_url")]
        public string SampleUrl { get; set; }

        [JsonProperty("sample_width")]
        public string SampleWidth { get; set; }

        [JsonProperty("sample_height")]
        public string SampleHeight { get; set; }

        [JsonProperty("sample_file_size")]
        public string SampleFileSize { get; set; }

        [JsonProperty("jpeg_url")]
        public string JpegUrl { get; set; }

        [JsonProperty("jpeg_width")]
        public string JpegWidth { get; set; }

        [JsonProperty("jpeg_height")]
        public string JpegHeight { get; set; }

        [JsonProperty("jpeg_file_size")]
        public string JpegFileSize { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("has_children")]
        public string HasChildren { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("is_held")]
        public string IsHeld { get; set; }

        [JsonProperty("frames_pending_string")]
        public string FramesPendingString { get; set; }

        [JsonProperty("frames_pending")]
        public List<string> FramesPending { get; set; }

        [JsonProperty("frames_string")]
        public string FramesString { get; set; }

        [JsonProperty("frames")]
        public List<string> Frames { get; set; }
    }
}