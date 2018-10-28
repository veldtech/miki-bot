using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;
using System;

namespace Miki.API.Imageboards.Objects
{
	public class DanbooruPost : BooruPost, ILinkable
	{
		[JsonProperty("id")]
		public uint Id { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonProperty("uploader_id")]
		public uint UploaderId { get; set; }

		[JsonProperty("source")]
		public string Source { get; set; }

		[JsonProperty("md5")]
		public string Md5Hash { get; set; }

		[JsonProperty("last_comment_bumped_at")]
		public DateTime? LastCommentBumpedAt { get; set; }

		[JsonProperty("rating")]
		public string Rating { get; set; }

		[JsonProperty("image_width")]
		private string ImageWidth { set { Width = value; } }

		[JsonProperty("image_height")]
		private string ImageHeight { set { Height = value; } }

		[JsonProperty("tag_string")]
		private string TagString { set { Tags = value; } }

		[JsonProperty("is_note_locked")]
		public bool IsNoteLocked { get; set; }

		[JsonProperty("fav_count")]
		public int FavouriteCount { get; set; }

		[JsonProperty("file_ext")]
		public string FileExtension { get; set; }

		[JsonProperty("last_noted_at")]
		public string LastNotedAt { get; set; }

		[JsonProperty("is_rating_locked")]
		public bool IsRatingLocked { get; set; }

		public string Url => FileUrl;

		public string SourceUrl => Source;

		public string Provider => "Danbooru";

		[JsonProperty("parent_id")]
		public object ParentId { get; set; }

		[JsonProperty("has_children")]
		public bool HasChildren { get; set; }

		[JsonProperty("approver_id")]
		public object ApproverId { get; set; }

		[JsonProperty("tag_count_general")]
		public long TagCountGeneral { get; set; }

		[JsonProperty("tag_count_artist")]
		public long TagCountArtist { get; set; }

		[JsonProperty("tag_count_character")]
		public long TagCountCharacter { get; set; }

		[JsonProperty("tag_count_copyright")]
		public long TagCountCopyright { get; set; }

		[JsonProperty("file_size")]
		public long FileSize { get; set; }

		[JsonProperty("is_status_locked")]
		public bool IsStatusLocked { get; set; }

		[JsonProperty("pool_string")]
		public string PoolString { get; set; }

		[JsonProperty("up_score")]
		public long UpScore { get; set; }

		[JsonProperty("down_score")]
		public long DownScore { get; set; }

		[JsonProperty("is_pending")]
		public bool IsPending { get; set; }

		[JsonProperty("is_flagged")]
		public bool IsFlagged { get; set; }

		[JsonProperty("is_deleted")]
		public bool IsDeleted { get; set; }

		[JsonProperty("tag_count")]
		public long TagCount { get; set; }

		[JsonProperty("updated_at")]
		public DateTimeOffset UpdatedAt { get; set; }

		[JsonProperty("is_banned")]
		public bool IsBanned { get; set; }

		[JsonProperty("pixiv_id")]
		public long? PixivId { get; set; }

		[JsonProperty("last_commented_at")]
		public DateTime? LastCommentedAt { get; set; }

		[JsonProperty("has_active_children")]
		public bool HasActiveChildren { get; set; }

		[JsonProperty("bit_flags")]
		public long BitFlags { get; set; }

		[JsonProperty("tag_count_meta")]
		public long TagCountMeta { get; set; }

		[JsonProperty("keeper_data")]
		public DanbooruKeeperSettings KeeperData { get; set; }

		[JsonProperty("uploader_name")]
		public string UploaderName { get; set; }

		[JsonProperty("has_large")]
		public bool HasLarge { get; set; }

		[JsonProperty("has_visible_children")]
		public bool HasVisibleChildren { get; set; }

		[JsonProperty("children_ids")]
		public string ChildrenIds { get; set; }

		[JsonProperty("is_favorited")]
		public bool IsFavorited { get; set; }

		[JsonProperty("tag_string_general")]
		public string TagStringGeneral { get; set; }

		[JsonProperty("tag_string_character")]
		public string TagStringCharacter { get; set; }

		[JsonProperty("tag_string_copyright")]
		public string TagStringCopyright { get; set; }

		[JsonProperty("tag_string_artist")]
		public string TagStringArtist { get; set; }

		[JsonProperty("tag_string_meta")]
		public string TagStringMeta { get; set; }

		[JsonProperty("file_url")]
		public string FileUrl { get; set; }

		[JsonProperty("large_file_url")]
		public string LargeFileUrl { get; set; }

		[JsonProperty("preview_file_url")]
		public string PreviewFileUrl { get; set; }
	}

	public class DanbooruKeeperSettings
	{
		[JsonProperty("uid")]
		public long Uid { get; set; }
	}
}