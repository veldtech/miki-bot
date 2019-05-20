using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Events;
using Miki.Modules.Overwatch.API;
using Miki.Modules.Overwatch.Objects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch
{
	[Module("Games")]
	internal class GamingModule
	{
		[Command("osu")]
		public async Task SendOsuSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.GetChannel().SendFileAsync(mem, $"sig.png");
				}
			}
		}

		[Command("ctb")]
		public async Task SendCatchTheBeatSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=2&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.GetChannel().SendFileAsync(mem, $"{username}.png");
				}
			}
		}

		[Command("mania")]
		public async Task SendManiaSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=3&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.GetChannel().SendFileAsync(mem, $"sig.png");
				}
			}
		}

		[Command("taiko")]
		public async Task SendTaikoSignatureAsync(IContext e)
		{
            e.GetArgumentPack().Take(out string username);

            using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData($"http://lemmmy.pw/osusig/sig.php?colour=pink&uname={username}&mode=1&countryrank");

				using (MemoryStream mem = new MemoryStream(data))
				{
					await e.GetChannel().SendFileAsync(mem, $"sig.png");
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

		public async Task<OverwatchUserResponse> InternalGetUser(IContext e, string[] username)
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