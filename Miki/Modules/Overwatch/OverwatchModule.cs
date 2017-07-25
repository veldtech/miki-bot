using IA.Events.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch
{
    [Module("Overwatch")]
    class OverwatchModule
    {
        [Command(Name = "overwatchstats", Aliases = new string[] { "owstats" })]
        public void OverwatchStatsAsync(EventContext e)
        {

        }
    }
}
