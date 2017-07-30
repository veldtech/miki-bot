using IA.Events.Attributes;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Modules.Overwatch.API;
using Miki.Modules.Overwatch.Objects;
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
        [Command(Name = "overwatchuser", Aliases = new string[] { "owuser" })]
        public async Task OverwatchStatsAsync(EventContext e)
        {
            string[] arguments = e.arguments.Split(' ');

            string[] username = arguments
                .Where(x => x.Contains("#"))
                .FirstOrDefault()
                .Split('#');

            string[] toggles = arguments
                .Where(x => x.StartsWith("-"))
                .ToArray();

            OverwatchUserResponse user = await InternalGetUser(e, username);

            if(user != null)
            {
                bool competitive = toggles.Contains("-c");

                OverwatchRegion region;
                if (toggles.Contains("-eu"))
                {
                    region = user.Europe;
                }
                else if(toggles.Contains("-us"))
                {
                    region = user.America;
                }
                else if (toggles.Contains("-kr"))
                {
                    region = user.Korea;
                }
                else
                {
                    region = GetBestRegion(user, competitive);
                }

                IDiscordEmbed embed = Utils.Embed
                    .SetTitle(string.Join("#", username) + "'s Overwatch Profile");

                if (competitive)
                {
                    var orderedPlaytime = region.heroes.playtime.competitive.OrderByDescending(x => x.Value);

                    int seconds = (int)Math.Round(orderedPlaytime.First().Value * 60 * 60);

                    await embed
                        .SetThumbnailUrl(region.stats.competitive.OverallStats.avatar)
                        .AddInlineField("MMR", region.stats.competitive.OverallStats.tier.ToString() + " (" + region.stats.competitive.OverallStats.comprank.ToString() + ")")
                        .AddInlineField("KDA ratio", Math.Round(region.stats.competitive.GameStats.eliminations / region.stats.competitive.GameStats.deaths, 2).ToString())
                        .AddInlineField("Favourite Character", orderedPlaytime.First().Key + " with " + Utils.ToTimeString(seconds))
                        .AddInlineField("Winrate", (region.stats.competitive.OverallStats.win_rate) + "%")
                        
                        .SendToChannel(e.Channel);
                }
                else
                {

                }
            }
            else
            {
                // couldnt find xd
            }
            
        }

        // worst function ever.
        public OverwatchRegion GetBestRegion(OverwatchUserResponse u, bool compo)
        {
            float? timePlayed = 0f;
            OverwatchRegion r = null;

            if(u.America != null)
            {
                timePlayed = (compo) ? u.America.heroes.playtime.competitive?.Sum(x => x.Value) : u.America.heroes.playtime.quickplay?.Sum(x => x.Value);
                r = u.America;
            }

            if(u.Europe != null)
            {
                float? t = (compo) ? u.Europe.heroes.playtime.competitive?.Sum(x => x.Value) : u.Europe.heroes.playtime.quickplay?.Sum(x => x.Value);
                if(t > timePlayed || t == null)
                {
                    timePlayed = t;
                    r = u.Europe;
                }
            }

            if (u.Korea != null)
            {
                float? t = (compo) ? u.Korea.heroes.playtime.competitive?.Sum(x => x.Value) : u.Korea.heroes.playtime.quickplay?.Sum(x => x.Value);
                if (t > timePlayed || t == null)
                {
                    timePlayed = t;
                    r = u.Korea;
                }
            }

            return r;
        }

        public async Task<OverwatchUserResponse> InternalGetUser(EventContext e, string[] username)
        {
            if (username.Length <= 1)
            {
                // no discriminator
                return null;
            }

            if (int.TryParse(username[1], out int descriminator))
            {
                string name = username[0];
                OverwatchUserResponse user = await OverwatchAPI.GetUser(name, descriminator);
                return user;
            }
            else
            {
                // no discriminator
                return null;
            }
        }
    }
}
