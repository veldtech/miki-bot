using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    public class RocketLeagueOptions
    {
        /// <summary>
        /// Your API key. You need this to use this library.
        /// </summary>
        public string ApiKey = "";
        
        /// <summary>
        /// The delay for the caches to renew them. Default: 24 hours
        /// </summary>
        public TimeSpan CacheTime = new TimeSpan(1, 0, 0, 0, 0);
    }
}