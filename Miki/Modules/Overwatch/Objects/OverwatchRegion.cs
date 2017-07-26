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
        public OverwatchHeroes heroes { get; set; }
        public OverwatchStats stats { get; set; }
        public OverwatchAchievements achievements { get; set; }
    }
}
