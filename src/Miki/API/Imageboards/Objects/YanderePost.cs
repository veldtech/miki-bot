namespace Miki.API.Imageboards.Objects
{
    using Miki.API.Imageboards.Interfaces;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    internal class YanderePost : BooruPost, ILinkable
    {
        public string Url => FileUrl;
        public string SourceUrl => Source;
        public string Provider => "Yande.re";

        [DataMember(Name = "created_at")]
        public int CreatedAt { get; set; }

        [DataMember(Name = "updated_at")]
        public int UpdatedAt { get; set; }

        [DataMember(Name = "creator_id")]
        public int CreatorId { get; set; }

        [DataMember(Name = "approver_id")]
        public object ApproverId { get; set; }

        [DataMember(Name = "author")]
        public string Author { get; set; }

        [DataMember(Name = "change")]
        public int Change { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "md5")]
        public string Md5 { get; set; }

        [DataMember(Name = "file_size")]
        public int FileSize { get; set; }

        [DataMember(Name = "file_ext")]
        public string FileExt { get; set; }

        [DataMember(Name = "file_url")]
        public string FileUrl { get; set; }

        [DataMember(Name = "is_shown_in_index")]
        public bool IsShownInIndex { get; set; }

        [DataMember(Name = "preview_url")]
        public string PreviewUrl { get; set; }

        [DataMember(Name = "preview_width")]
        public int PreviewWidth { get; set; }

        [DataMember(Name = "preview_height")]
        public int PreviewHeight { get; set; }

        [DataMember(Name = "actual_preview_width")]
        public int ActualPreviewWidth { get; set; }

        [DataMember(Name = "actual_preview_height")]
        public int ActualPreviewHeight { get; set; }

        [DataMember(Name = "sample_url")]
        public string SampleUrl { get; set; }

        [DataMember(Name = "sample_width")]
        public int SampleWidth { get; set; }

        [DataMember(Name = "sample_height")]
        public int SampleHeight { get; set; }

        [DataMember(Name = "sample_file_size")]
        public int SampleFileSize { get; set; }

        [DataMember(Name = "jpeg_url")]
        public string JpegUrl { get; set; }

        [DataMember(Name = "jpeg_width")]
        public int JpegWidth { get; set; }

        [DataMember(Name = "jpeg_height")]
        public int JpegHeight { get; set; }

        [DataMember(Name = "jpeg_file_size")]
        public int JpegFileSize { get; set; }

        [DataMember(Name = "rating")]
        public string Rating { get; set; }

        [DataMember(Name = "is_rating_locked")]
        public bool IsRatingLocked { get; set; }

        [DataMember(Name = "has_children")]
        public bool HasChildren { get; set; }

        [DataMember(Name = "parent_id")]
        public object ParentId { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "is_pending")]
        public bool IsPending { get; set; }

        [DataMember(Name = "is_held")]
        public bool IsHeld { get; set; }

        [DataMember(Name = "frames_pending_string")]
        public string FramesPendingString { get; set; }

        [DataMember(Name = "frames_pending")]
        public List<object> FramesPending { get; set; }

        [DataMember(Name = "frames_string")]
        public string FramesString { get; set; }

        [DataMember(Name = "frames")]
        public List<object> Frames { get; set; }

        [DataMember(Name = "is_note_locked")]
        public bool IsNoteLocked { get; set; }

        [DataMember(Name = "last_noted_at")]
        public int LastNotedAt { get; set; }

        [DataMember(Name = "last_commented_at")]
        public int LastCommentedAt { get; set; }
    }
}