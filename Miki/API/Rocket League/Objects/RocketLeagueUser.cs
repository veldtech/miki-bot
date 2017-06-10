using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.RocketLeague
{
    public class RocketLeagueUser
    {
        [JsonProperty("uniqueId")]
        public string UniqueId { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("platform")]
        public RocketLeaguePlatform Platform { get; set; }

        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; }

        [JsonProperty("profileUrl")]
        public string ProfileUrl { get; set; }

        [JsonProperty("signatureUrl")]
        public string SignatureUrl { get; set; }

        public Dictionary<int, Dictionary<int, RocketLeagueRankedStats>> RankedSeasons { get; set; }

        internal ulong? LastRequested { get; set; }
        internal ulong? CreatedAt { get; set; }
        internal ulong? UpdatedAt { get; set; }
        internal ulong? NextUpdateAt { get; set; }
        internal ulong? UpdatedInfoAt { get; set; }
    }
}
