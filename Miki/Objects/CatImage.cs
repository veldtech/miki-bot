using Newtonsoft.Json;

namespace Miki.Objects
{
    public class CatImage
    {
        [JsonProperty("file")]
        public string File { get; set; }
    }
}