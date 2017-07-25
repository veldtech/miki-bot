using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch.Objects
{
    public class OverwatchOverallStats
    {
        public object tier { get; set; }
        public string avatar { get; set; }
        public int wins { get; set; }
        public int level { get; set; }
        public double win_rate { get; set; }
        public int losses { get; set; }
        public string rank_image { get; set; }
        public int prestige { get; set; }
        public int games { get; set; }
        public object comprank { get; set; }
    }
}
