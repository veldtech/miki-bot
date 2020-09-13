using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Miki.API.Imageboards.Objects
{
    public class YanderePost : BooruPost, ILinkable
    {
        public string Url => FileUrl ?? Source;
        public string SourceUrl => Source;
        public string Provider => "Yande.re";

        [JsonProperty("created_at")]
        public int CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public int UpdatedAt { get; set; }

        [JsonProperty("creator_id")]
        public int CreatorId { get; set; }

        [JsonProperty("approver_id")]
        public object ApproverId { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("change")]
        public int Change { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("file_size")]
        public int FileSize { get; set; }

        [JsonProperty("file_ext")]
        public string FileExt { get; set; }

        [JsonProperty("file_url")]
        public string FileUrl { get; set; }

        [JsonProperty("is_shown_in_index")]
        public bool IsShownInIndex { get; set; }

        [JsonProperty("preview_url")]
        public string PreviewUrl { get; set; }

        [JsonProperty("preview_width")]
        public int PreviewWidth { get; set; }

        [JsonProperty("preview_height")]
        public int PreviewHeight { get; set; }

        [JsonProperty("actual_preview_width")]
        public int ActualPreviewWidth { get; set; }

        [JsonProperty("actual_preview_height")]
        public int ActualPreviewHeight { get; set; }

        [JsonProperty("sample_url")]
        public string SampleUrl { get; set; }

        [JsonProperty("sample_width")]
        public int SampleWidth { get; set; }

        [JsonProperty("sample_height")]
        public int SampleHeight { get; set; }

        [JsonProperty("sample_file_size")]
        public int SampleFileSize { get; set; }

        [JsonProperty("jpeg_url")]
        public string JpegUrl { get; set; }

        [JsonProperty("jpeg_width")]
        public int JpegWidth { get; set; }

        [JsonProperty("jpeg_height")]
        public int JpegHeight { get; set; }

        [JsonProperty("jpeg_file_size")]
        public int JpegFileSize { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("is_rating_locked")]
        public bool IsRatingLocked { get; set; }

        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }

        [JsonProperty("parent_id")]
        public object ParentId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("is_pending")]
        public bool IsPending { get; set; }

        [JsonProperty("is_held")]
        public bool IsHeld { get; set; }

        [JsonProperty("frames_pending_string")]
        public string FramesPendingString { get; set; }

        [JsonProperty("frames_pending")]
        public List<object> FramesPending { get; set; }

        [JsonProperty("frames_string")]
        public string FramesString { get; set; }

        [JsonProperty("frames")]
        public List<object> Frames { get; set; }

        [JsonProperty("is_note_locked")]
        public bool IsNoteLocked { get; set; }

        [JsonProperty("last_noted_at")]
        public int LastNotedAt { get; set; }

        [JsonProperty("last_commented_at")]
        public int LastCommentedAt { get; set; }
    }
}