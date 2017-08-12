using System.Collections.Generic;

namespace Miki.Modules.Overwatch.Objects
{
    public class OverwatchPlaytime
    {
        public Dictionary<string, float> quickplay = new Dictionary<string, float>();
        public Dictionary<string, float> competitive = new Dictionary<string, float>();
    }
}