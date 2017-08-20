using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Miki.API.Imageboards;
using Miki.API.Imageboards.Interfaces;

namespace Miki.Objects
{
    public class BooruPost : ILinkable
    {
        public string Url => "";

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

        [JsonProperty("score")]
        public string Score { get; set; }
    }

    internal class E621Post : BooruPost, ILinkable
    {
        public new string Url => FileUrl;

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
        public List<string> Children { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        [JsonProperty("artist")]
        public List<string> Artist { get; set; } = new List<string>();

        [JsonProperty("sources")]
        public List<string> Sources { get; set; } = new List<string>();
    }

    internal class GelbooruPost : BooruPost, ILinkable
    {
        public new string Url => "http:" + FileUrl;

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

    internal class ImgurPost
    {
        [JsonProperty("data")]
        public List<ImgurImage> Entries { get; set; }

        [JsonProperty("success")]
        public string Success { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal class KonachanPost : BooruPost, ILinkable
    {
        public new string Url => "http:" + FileUrl;

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

    internal class Rule34Post : BooruPost, ILinkable
    {
        public new string Url => $"http://img.rule34.xxx/images/{Directory}/{Image}";

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

    internal class YanderePost
    {
    }
}