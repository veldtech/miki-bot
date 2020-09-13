using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Miki.API.Imageboards.Objects
{
    public class E621Post : BooruPost, ILinkable
    {
        public string Url => File.Url;
        public string SourceUrl => Sources[0] ?? "";
        public string Provider => "E621";
        public new string Tags => string.Join(" ", AllTags.General);
        public new string Score => AllScore.Total.ToString();

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("file")]
        public File File { get; set; }

        [JsonProperty("preview")]
        public Preview Preview { get; set; }

        [JsonProperty("sample")]
        public Sample Sample { get; set; }

        [JsonProperty("score")]
        public Score AllScore { get; set; }

        [JsonProperty("tags")]
        public Tags AllTags { get; set; }

        [JsonProperty("locked_tags")]
        public List<string> LockedTags { get; set; }

        [JsonProperty("change_seq")]
        public int ChangeSeq { get; set; }

        [JsonProperty("flags")]
        public Flags Flags { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }

        [JsonProperty("fav_count")]
        public int FavCount { get; set; }

        [JsonProperty("sources")]
        public List<string> Sources { get; set; }

        [JsonProperty("pools")]
        public List<int> Pools { get; set; }

        [JsonProperty("relationships")]
        public Relationships Relationships { get; set; }

        [JsonProperty("approver_id")]
        public int? ApproverId { get; set; }

        [JsonProperty("uploader_id")]
        public int UploaderId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }

        [JsonProperty("is_favorited")]
        public bool IsFavorited { get; set; }

        [JsonProperty("has_notes")]
        public bool HasNotes { get; set; }
    }

    public class File
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Preview
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Sample
    {
        [JsonProperty("has")]
        public bool Has { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Score
    {
        [JsonProperty("up")]
        public int Up { get; set; }

        [JsonProperty("down")]
        public int Down { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class Tags
    {
        [JsonProperty("general")]
        public List<string> General { get; set; }

        [JsonProperty("species")]
        public List<string> Species { get; set; }

        [JsonProperty("character")]
        public List<string> Character { get; set; }

        [JsonProperty("copyright")]
        public List<string> Copyright { get; set; }

        [JsonProperty("artist")]
        public List<string> Artist { get; set; }

        [JsonProperty("invalid")]
        public List<string> Invalid { get; set; }

        [JsonProperty("lore")]
        public List<string> Lore { get; set; }

        [JsonProperty("meta")]
        public List<string> Meta { get; set; }
    }

    public class Flags
    {
        [JsonProperty("pending")]
        public bool Pending { get; set; }

        [JsonProperty("flagged")]
        public bool Flagged { get; set; }

        [JsonProperty("note_locked")]
        public bool NoteLocked { get; set; }

        [JsonProperty("status_locked")]
        public bool StatusLocked { get; set; }

        [JsonProperty("rating_locked")]
        public bool RatingLocked { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
    }

    public class Relationships
    {
        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("has_children")]
        public bool HasChildren { get; set; }

        [JsonProperty("has_active_children")]
        public bool HasActiveChildren { get; set; }

        [JsonProperty("children")]
        public List<int>? Children { get; set; }
    }
}