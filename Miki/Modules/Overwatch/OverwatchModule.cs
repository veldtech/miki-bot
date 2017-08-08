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

            if(user.Request != null)
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

                OverwatchUserContext c = new OverwatchUserContext(competitive, region);

                if(!c.isValid)
                {
                    await e.ErrorEmbed("The user specified does not exist, or hasn't played on this specific region.")
                        .SendToChannel(e.Channel);
                    return;
                }

                IDiscordEmbed embed = Utils.Embed
                    .SetTitle(string.Join("#", username) + "'s Overwatch Profile")
                    .SetThumbnailUrl(c.Stats.OverallStats.avatar);

                    var orderedPlaytime = c.PlayTime.OrderByDescending(x => x.Value);

                    float seconds = orderedPlaytime.First().Value.FromHoursToSeconds();

                if (c.isCompetitive)
                {
                    embed.AddInlineField("MMR", c.Stats.OverallStats.tier.ToString() + " (" + c.Stats.OverallStats.comprank.ToString() + ")")
                         .AddInlineField("Winrate", (c.Stats.OverallStats.win_rate) + "%");
                }

                embed.AddInlineField("Favourite Character", orderedPlaytime.First().Key + " with " + Utils.ToTimeString(seconds, e.Channel.GetLocale()))
                     .AddInlineField("K/D/A ratio", Math.Round(c.Stats.GameStats.eliminations / c.Stats.GameStats.deaths, 2))
                     .AddInlineField("Time Played", c.Stats.GameStats.time_played + " hours")
                     .AddInlineField("Objective Time", c.Stats.GameStats.objective_time.FromHoursToSeconds().ToTimeString(e.Channel.GetLocale(), true));
                
                await embed.SendToChannel(e.Channel);
            }
            else
            {
                await e.ErrorEmbed("The user specified does not exist!")
                    .SendToChannel(e.Channel);
                return;
            }
            
        }

        // still a bad function, but i digress
        public OverwatchRegion GetBestRegion(OverwatchUserResponse u, bool compo)
        {
            List<OverwatchRegion> regions = new List<OverwatchRegion>();
            regions.Add(u.America);
            regions.Add(u.Europe);
            regions.Add(u.Korea);

            return regions.OrderByDescending(x =>
            {
                float? value;
                if (compo)
                {
                    value = x?.heroes?.playtime?.competitive?.Sum(y => y.Value);
                }
                else
                {
                    value = x?.heroes?.playtime?.quickplay?.Sum(y => y.Value);
                }
                return value == null ? 0 : value;
            }).FirstOrDefault();
        }

        public async Task<OverwatchUserResponse> InternalGetUser(EventContext e, string[] username)
        {
            if (username.Length <= 1)
            {
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
                return null;
            }
        }
    }
}
