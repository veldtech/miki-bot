using Miki.Framework.Events.Attributes;
using Miki.Modules.Overwatch.API;
using Miki.Modules.Overwatch.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Miki.Common;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using Miki.Discord.Common;

namespace Miki.Modules.Overwatch
{
    [Module("Gaming")]
    internal class GamingModule
    {
		[Command(Name = "osu")]
		public async Task SendOsuSignatureAsync(EventContext e)
		{
			string username = e.Arguments.FirstOrDefault()?.Argument ?? "Veld";

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await (e.Channel as IDiscordTextChannel).SendFileAsync(mem, $"sig.png");
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
					await (e.Channel as IDiscordTextChannel).SendFileAsync(mem, $"{username}.png");
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
					await (e.Channel as IDiscordTextChannel).SendFileAsync(mem, $"sig.png");
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
					await (e.Channel as IDiscordTextChannel).SendFileAsync(mem, $"sig.png");
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