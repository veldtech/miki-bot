using Newtonsoft.Json;

namespace Miki.Objects
{
    internal class OverwatchUserData
    {
        /// <summary>
        /// User's username in-game
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Level bound to their account
        /// </summary>
        [JsonProperty("level")]
        public string Level { get; set; }

        /// <summary>
        /// Avatar image
        /// </summary>
        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        /// <summary>
        /// Trim around avatar.
        /// </summary>
        [JsonProperty("levelframe")]
        public string LevelFrame { get; set; }

        /// <summary>
        /// Stars on top of trim.
        /// </summary>
        [JsonProperty("star")]
        public string Star { get; set; }
    }
}