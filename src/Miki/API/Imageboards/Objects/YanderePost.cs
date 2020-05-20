using Miki.API.Imageboards.Interfaces;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Miki.API.Imageboards.Objects
{
    /*
     *  {
      "id": 614834,
      "tags": "pantsu pantyhose shirt_lift underboob yom",
      "created_at": 1583104349,
      "updated_at": 1583104370,
      "creator_id": 25882,
      "approver_id": null,
      "author": "Mr_GT",
      "change": 3235698,
      "source": "https://twitter.com/i/web/status/1234252711807213571",
      "score": 6,
      "md5": "da630593cbf6cfe01b868c1ad1632a16",
      "file_size": 187671,
      "file_ext": "jpg",
      "file_url": "https://files.yande.re/image/da630593cbf6cfe01b868c1ad1632a16/yande.re%20614834%20pantsu%20pantyhose%20shirt_lift%20underboob%20yom.jpg",
      "is_shown_in_index": true,
      "preview_url": "https://assets.yande.re/data/preview/da/63/da630593cbf6cfe01b868c1ad1632a16.jpg",
      "preview_width": 107,
      "preview_height": 150,
      "actual_preview_width": 215,
      "actual_preview_height": 300,
      "sample_url": "https://files.yande.re/sample/da630593cbf6cfe01b868c1ad1632a16/yande.re%20614834%20sample%20pantsu%20pantyhose%20shirt_lift%20underboob%20yom.jpg",
      "sample_width": 1073,
      "sample_height": 1500,
      "sample_file_size": 217528,
      "jpeg_url": "https://files.yande.re/image/da630593cbf6cfe01b868c1ad1632a16/yande.re%20614834%20pantsu%20pantyhose%20shirt_lift%20underboob%20yom.jpg",
      "jpeg_width": 1287,
      "jpeg_height": 1800,
      "jpeg_file_size": 0,
      "rating": "q",
      "is_rating_locked": false,
      "has_children": false,
      "parent_id": null,
      "status": "active",
      "is_pending": false,
      "width": 1287,
      "height": 1800,
      "is_held": false,
      "frames_pending_string": "",
      "frames_pending": [
        
      ],
      "frames_string": "",
      "frames": [
        
      ],
      "is_note_locked": false,
      "last_noted_at": 0,
      "last_commented_at": 0
    },
     */
    public class YanderePost : BooruPost, ILinkable
    {
        public string Url => FileUrl ?? Source;
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