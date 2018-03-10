using Miki.Framework.Events.Attributes;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Modules.Overwatch.API;
using Miki.Modules.Overwatch.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Miki.Common;

namespace Miki.Modules.Overwatch
{
    [Module("Gaming")]
    internal class GamingModule
    {
        [Command(Name = "overwatchuser", Aliases = new string[] { "owuser" })]
        public async Task OverwatchStatsAsync(EventContext e)
        {
			ArgObject args = e.Arguments.FirstOrDefault();

			if(args == null)
			{
				// TODO: add argument error message
				return;
			}

            string[] username = args.Argument
                .Split('#');

			args = args.Next();

			// Probably make this better
			string[] toggles = args?.TakeUntilEnd()
				.Argument
				.Split(' ')
				.Where(x => x.StartsWith("-"))
				.ToArray() ?? new string[0];

            OverwatchUserResponse user = await InternalGetUser(e, username);

            if (user.Request != null)
            {
                bool competitive = toggles.Contains("-c");

                OverwatchRegion region;
                if (toggles.Contains("-eu"))
                {
                    region = user.Europe;
                }
                else if (toggles.Contains("-us"))
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

                if (!c.isValid)
                {
                    e.ErrorEmbed("The user specified does not exist, or hasn't played on this specific region.")
                        .QueueToChannel(e.Channel);
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

                embed.QueueToChannel(e.Channel);
            }
            else
            {
                e.ErrorEmbed("The user specified does not exist!")
                    .QueueToChannel(e.Channel);
                return;
            }
        }

		[Command(Name = "osu")]
		public async Task SendOsuSignatureAsync(EventContext e)
		{
			string username = e.Arguments.FirstOrDefault()?.Argument ?? "Veld";

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.Channel.SendFileAsync(mem, $"sig.png");
				}
			}
		}

		[Command(Name = "ctb")]
		public async Task SendCatchTheBeatSignatureAsync(EventContext e)
		{
			string username = e.Arguments.FirstOrDefault()?.Argument ?? "Veld";

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=2&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.Channel.SendFileAsync(mem, $"{username}.png");
				}
			}
		}

		[Command(Name = "mania")]
		public async Task SendManiaSignatureAsync(EventContext e)
		{
			string username = e.Arguments.FirstOrDefault()?.Argument ?? "Veld";

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=3&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.Channel.SendFileAsync(mem, $"sig.png");
				}
			}
		}

		[Command(Name = "taiko")]
		public async Task SendTaikoSignatureAsync(EventContext e)
		{
			string username = e.Arguments.FirstOrDefault()?.Argument ?? "Veld";

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=1&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.Channel.SendFileAsync(mem, $"sig.png");
				}
			}
		}

		public OverwatchRegion GetBestRegion(OverwatchUserResponse u, bool compo)
        {
            List<OverwatchRegion> regions = new List<OverwatchRegion>
            {
                u.America,
                u.Europe,
                u.Korea
            };

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
                return value ?? 0;
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