using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch.Objects
{
    class OverwatchRegion
    {
        [JsonProperty("competitive")]
        public OverwatchGamemode Competitive { get; set; }

        [JsonProperty("quickplay")]
        public OverwatchGamemode Quickplay { get; set; }
    }
}
