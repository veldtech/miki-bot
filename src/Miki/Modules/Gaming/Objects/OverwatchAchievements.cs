using System.Collections.Generic;

namespace Miki.Modules.Overwatch.Objects
{
    public class OverwatchAchievements
    {
        public Dictionary<string, bool> maps = new Dictionary<string, bool>();
        public Dictionary<string, bool> general = new Dictionary<string, bool>();
        public Dictionary<string, bool> offense = new Dictionary<string, bool>();
        public Dictionary<string, bool> special = new Dictionary<string, bool>();
        public Dictionary<string, bool> defense = new Dictionary<string, bool>();
        public Dictionary<string, bool> tank = new Dictionary<string, bool>();
        public Dictionary<string, bool> support = new Dictionary<string, bool>();
    }
}