using Miki.API.Imageboards.Interfaces;
using Newtonsoft.Json;

namespace Miki.API.Imageboards.Objects
{
    public class CatImage : ILinkable
    {
        public string Url => File;
        public string Tags => "";
        public string SourceUrl => "";
        public string Score => MikiRandom.Next(1000).ToString();
        public string Provider => "Cat";

        [JsonProperty("file")]
        public string File { get; set; }
    }
}