namespace Miki.Services.Reddit
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class RedditPostData
    {
        [DataMember(Name = "subreddit", Order = 1)]
        public string Subreddit { get; set; }

        [DataMember(Name = "title", Order = 2)]
        public string Title { get; set; }

        [DataMember(Name = "url", Order = 3)]
        public string Url { get; set; }

        [DataMember(Name = "ups", Order = 4)]
        public int Upvotes { get; set; }

        [DataMember(Name = "permalink", Order = 5)]
        public string PermaLink { get; set; }

        [DataMember(Name = "num_comments", Order = 6)]
        public string Comments { get; set; }

        [DataMember(Name = "over_18", Order = 7)]
        public bool Nsfw { get; set; }

        [DataMember(Name = "media", Order = 8)]
        public RedditMedia Media { get; set; }

        [DataMember(Name = "post_hint", Order = 9)]
        public string PostHint { get; set; }

        [DataMember(Name = "preview", Order = 10)]
        public RedditPreviewData Previews { get; set; }
    }

    [DataContract]
    public class RedditPreviewData
    {
        [DataMember(Name = "images", Order = 1)]
        public IEnumerable<RedditImagePreview> Images { get; set; }
    }

    [DataContract]
    public class RedditImagePreview
    {
        [DataMember(Name = "source", Order = 1)]
        public RedditResource Source { get; set; }

        [DataMember(Name = "resolutions", Order = 2)]
        public IEnumerable<RedditResource> Resolutions { get; set; }

        [DataMember(Name = "variants", Order = 3)]
        public RedditImagePreviewVariants Variants { get; set; }

    }

    [DataContract]
    public class RedditImagePreviewVariants
    {
        [DataMember(Name = "obfuscated", Order = 1)]
        public RedditImagePreview Obfuscated { get; set; }

        [DataMember(Name = "gif", Order = 2)]
        public RedditImagePreview Gif { get; set; }
    }

    [DataContract]
    public class RedditResource
    {

        [DataMember(Name = "url", Order = 1)]
        public string Url { get; set; }
        
        [DataMember(Name = "height", Order = 2)]
        public int Height { get; set; }

        [DataMember(Name = "width", Order = 3)]
        public int Width { get; set; }
    }

    [DataContract]
    public class RedditMedia
    {
        [DataMember(Name = "type", Order = 1)]
        public string Type { get; set; }

        [DataMember(Name = "oembed", Order = 2)]
        public RedditMediaEmbed Embed { get; set; }

        [DataMember(Name = "is_video", Order = 3)]
        public bool IsVideo { get; set; }

    }

    public class RedditMediaEmbed
    {
        [DataMember(Name = "thumbnail_url", Order = 12)]
        public string ThumbnailUrl { get; set; }
    }
}