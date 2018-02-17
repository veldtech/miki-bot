using Newtonsoft.Json;

namespace Miki.API.UrbanDictionary
{
    public class UrbanDictionaryEntry
    {
        [JsonProperty("definition")]
        public string Definition { get; set; }

        [JsonProperty("permalink")]
        public string Permalink { get; set; }

        [JsonProperty("thumbs_up")]
        public int ThumbsUp { get; set; }

        [JsonProperty("thumbs_down")]
        public int ThumbsDown { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("word")]
        public string Term { get; set; }

        [JsonProperty("defid")]
        public string DefinitionId { get; set; }

        [JsonProperty("current_vote")]
        public string CurrentVote { get; set; }

        [JsonProperty("example")]
        public string Example { get; set; }

        public int Score => ThumbsUp - ThumbsDown;
        public string SearchUrl => "http://www.urbandictionary.com/define.php?term=" + Term;
    }
}