using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch.Objects
{
    public class OverwatchGamemode
    {
        [JsonProperty("average_stats")]
        public OverwatchAverageStats AverageStats { get; set; }

        [JsonProperty("competitive")]
        public bool IsCompetitive { get; set; }

        [JsonProperty("game_stats")]
        public OverwatchGameStats GameStats { get; set; }

        [JsonProperty("overall_stats")]
        public OverwatchOverallStats OverallStats { get; set; }
    }
}
