using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Miki.API.Imageboards.Interfaces;

namespace Miki.API.Imageboards.Objects
{
    public class CatImage : ILinkable
    {
        public string Url => File;

        [JsonProperty("file")]
        public string File { get; set; }
    }
}
