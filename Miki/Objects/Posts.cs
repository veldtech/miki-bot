using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

/// <summary>
/// All image-board api post objects
/// </summary>
namespace Miki.Objects
{
    public class BasePost
    {
        protected static List<string> bannedTags = new List<string>()
        {
            "loli",
            "shota",
            "gore",
            "vore",
            "death"
        };

        protected static string GetTags(string[] tags)
        {
            List<string> output = new List<string>();

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == "awoo")
                {
                    output.Add("inubashiri_momiji");
                    continue;
                }
                if (tags[i] == "miki")
                {
                    output.Add("sf-a2_miki");
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(tags[i]))
                {
                    output.Add(tags[i]);
                }
            }

            string outputTags = "";
            for (int i = 0; i < output.Count; i++)
            {
                outputTags += output[i] + "+";
            }
            outputTags.Remove(outputTags.Length - 1);
            return outputTags;
        }

        protected static void RemoveBannedTerms(List<string> tags)
        {
            tags.RemoveAll(p => bannedTags.Contains(p));
        }

        protected static void AddBannedTerms(List<string> tags)
        {
            bannedTags.ForEach(p => tags.Add("-" + p));
        }
    }

    internal interface IPost
    {
        string ImageUrl { get; }
    }

    internal class E621Post : BasePost, IPost
    {
        public string ImageUrl
        {
            get
            {
                return FileUrl;
            }
        }

        public static E621Post Create(string content, ImageRating r)
        {
            WebClient c = new WebClient();

            c.UseDefaultCredentials = true;
            c.Credentials = CredentialCache.DefaultCredentials;

            c.Headers.Add("User-Agent: Other");

            byte[] b;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            switch (r)
            {
                case ImageRating.EXPLICIT:
                    {
                        tags.Add("rating:e");
                    }
                    break;

                case ImageRating.QUESTIONABLE:
                    {
                        tags.Add("rating:q");
                    }
                    break;

                case ImageRating.SAFE:
                    {
                        tags.Add("rating:s");
                    }
                    break;
            }
            tags.AddRange(command);
            RemoveBannedTerms(tags);
            AddBannedTerms(tags);

            string outputTags = GetTags(tags.ToArray());

            b = c.DownloadData("http://e621.net/post/index.json?limit=1&tags=" + outputTags);

            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);
                List<E621Post> d = JsonConvert.DeserializeObject<List<E621Post>>(result);
                if (d != null)
                {
                    return d[Global.random.Next(0, d.Count)];
                }
            }
            return null;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

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

        [JsonProperty("score")]
        public string Score { get; set; }

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

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

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

    internal class GelbooruPost : BasePost, IPost
    {
        public string ImageUrl
        {
            get
            {
                return "http:" + FileUrl;
            }
        }

        public static GelbooruPost Create(string content, ImageRating r)
        {
            WebClient c = new WebClient();
            byte[] b;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            switch (r)
            {
                case ImageRating.EXPLICIT:
                    {
                        tags.Add("rating:explicit");
                    }
                    break;

                case ImageRating.QUESTIONABLE:
                    {
                        tags.Add("rating:questionable");
                    }
                    break;

                case ImageRating.SAFE:
                    {
                        tags.Add("rating:safe");
                    }
                    break;
            }
            tags.AddRange(command);
            RemoveBannedTerms(tags);
            AddBannedTerms(tags);

            string outputTags = GetTags(tags.ToArray());

            b = c.DownloadData("http://gelbooru.com/index.php?page=dapi&s=post&q=index&json=1&tags=" + outputTags);
            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);
                List<GelbooruPost> d = JsonConvert.DeserializeObject<List<GelbooruPost>>(result);
                if (d != null)
                {
                    return d[Global.random.Next(0, d.Count)];
                }
            }
            return null;
        }

        [JsonProperty("directory")]
        public string Directory { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

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

        [JsonProperty("score")]
        public string Score { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("file_url")]
        public string FileUrl { get; set; }
    }

    // TODO: Add ImgurPost.Create()
    internal class ImgurPost
    {
        [JsonProperty("data")]
        public List<ImgurImage> Entries { get; set; }

        [JsonProperty("success")]
        public string Success { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal class KonachanPost : BasePost, IPost
    {
        public string ImageUrl
        {
            get
            {
                return "http:" + FileUrl;
            }
        }

        public static KonachanPost Create(string content, ImageRating r)
        {
            WebClient c = new WebClient();
            byte[] b;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            switch (r)
            {
                case ImageRating.EXPLICIT:
                    {
                        tags.Add("rating:e");
                    }
                    break;

                case ImageRating.QUESTIONABLE:
                    {
                        tags.Add("rating:q");
                    }
                    break;

                case ImageRating.SAFE:
                    {
                        tags.Add("rating:s");
                    }
                    break;
            }

            tags.AddRange(command);
            RemoveBannedTerms(tags);
            AddBannedTerms(tags);

            string outputTags = GetTags(tags.ToArray());

            b = c.DownloadData($"https://konachan.com/post.json?tags={outputTags}");
            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);
                List<KonachanPost> d = JsonConvert.DeserializeObject<List<KonachanPost>>(result);
                if (d != null)
                {
                    return d[Global.random.Next(0, d.Count)];
                }
            }
            return null;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

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

        [JsonProperty("score")]
        public string Score { get; set; }

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

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

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

    internal class Rule34Post : BasePost, IPost
    {
        public string ImageUrl
        {
            get
            {
                return $"http://img.rule34.xxx/images/{Directory}/{Image}";
            }
        }

        public static Rule34Post Create(string content, ImageRating r)
        {
            WebClient c = new WebClient();
            byte[] b;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            switch (r)
            {
                case ImageRating.EXPLICIT:
                    {
                        tags.Add("rating:explicit");
                    }
                    break;

                case ImageRating.QUESTIONABLE:
                    {
                        tags.Add("rating:questionable");
                    }
                    break;

                case ImageRating.SAFE:
                    {
                        tags.Add("rating:safe");
                    }
                    break;
            }
            tags.AddRange(command);
            RemoveBannedTerms(tags);
            AddBannedTerms(tags);

            string outputTags = GetTags(tags.ToArray());

            b = c.DownloadData($"http://rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&tags={ outputTags }");
            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);
                List<Rule34Post> d = JsonConvert.DeserializeObject<List<Rule34Post>>(result);
                if (d != null)
                {
                    return d[Global.random.Next(0, d.Count)];
                }
            }
            return null;
        }

        [JsonProperty("directory")]
        public string Directory { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }

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

        [JsonProperty("score")]
        public string Score { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }
    }

    internal class SafebooruPost : BasePost, IPost
    {
        public string ImageUrl
        {
            get
            {
                return $"https://safebooru.org/{ ((Sample) ? "samples" : "images") }/{Directory}/{((Sample) ? "sample_" : "")}{Image}";
            }
        }

        public static SafebooruPost Create(string content, ImageRating r)
        {
            WebClient c = new WebClient();
            byte[] b;
            string[] command = content.Split(' ');

            List<string> tags = new List<string>();

            switch (r)
            {
                case ImageRating.EXPLICIT:
                    {
                        tags.Add("rating:explicit");
                    }
                    break;

                case ImageRating.QUESTIONABLE:
                    {
                        tags.Add("rating:questionable");
                    }
                    break;

                case ImageRating.SAFE:
                    {
                        tags.Add("rating:safe");
                    }
                    break;
            }

            tags.AddRange(command);
            RemoveBannedTerms(tags);
            AddBannedTerms(tags);

            string outputTags = GetTags(tags.ToArray());

            b = c.DownloadData($"https://safebooru.org/index.php?page=dapi&s=post&q=index&json=1&tags={ outputTags }");
            if (b != null)
            {
                string result = Encoding.UTF8.GetString(b);
                List<SafebooruPost> d = JsonConvert.DeserializeObject<List<SafebooruPost>>(result);
                if (d != null)
                {
                    return d[Global.random.Next(0, d.Count)];
                }
            }
            return null;
        }

        [JsonProperty("directory")]
        public string Directory { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

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

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }
    }

    internal enum ImageRating
    {
        NONE,
        SAFE,
        QUESTIONABLE,
        EXPLICIT
    }
}