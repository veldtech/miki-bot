namespace Miki.Extensions
{
    internal class OverwatchUserCompetitive
    {
        [Newtonsoft.Json.JsonProperty("rank")]
        public int Rank { get; set; }

        [Newtonsoft.Json.JsonProperty("rank_img")]
        public string RankImage { get; set; }
    }
}