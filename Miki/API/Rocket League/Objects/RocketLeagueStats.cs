using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    class RocketLeagueStats
    {
        public int Wins { get; internal set; }
        public int Goals { get; internal set; }
        public int Mvps { get; internal set; }
        public int Saves { get; internal set; }
        public int Shots { get; internal set; }
        public int Assists { get; internal set; }
    }
}
