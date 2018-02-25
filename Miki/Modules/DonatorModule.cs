using Miki.Framework;
using Miki.Framework.Events.Attributes;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Models;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Miki.Rest;
using System;
using Newtonsoft.Json;
using Miki.Common;
using Miki.Patreon.Types;

namespace Miki.Modules
{
    [Module(Name = "Donator")]
    internal class DonatorModule
    {
		RestClient client = new RestClient(Global.Config.ImageApiUrl)
			.AddHeader("Authorization", Global.Config.MikiApiKey);

		public DonatorModule()
		{
			/// patreon
			WebhookManager.OnEvent += async (value) =>
			{
				if(value.auth_code == "PATREON_UPDATE")
				{
					PatreonPledge patreonObject = JsonConvert.DeserializeObject<PatreonPledge>(value.data);

					ulong userId = ulong.Parse(patreonObject.Included.FirstOrDefault(x => x.Type == PatreonType.USER).attributes.ToObject<UserAttribute>().DiscordUserId ?? "0");
					string name = "";

					if(userId != 0)
					{
						IDiscordUser user = Bot.Instance.GetUser(userId);
						name = user.Username + "#" + user.Discriminator;
					}


					Utils.Embed.SetTitle("yo. some donation happened")
						.SetDescription(name + " donated " + ((double)patreonObject.Data.attributes.ToObject<PledgeAttribute>().AmountCents / 100) + "$")
						.QueueToChannel(266365180848504832);
				}
			};
		}

		[Command(Name = "redeemkey")]
		public async Task RedeemKeyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;
				DonatorKey key = await context.DonatorKey.FindAsync(Guid.Parse(e.arguments));
				IsDonator donatorStatus = await context.IsDonator.FindAsync(id);

				if (key != null)
				{
					if (donatorStatus == null)
					{
						donatorStatus = (await context.IsDonator.AddAsync(new IsDonator()
						{
							UserId = id
						})).Entity;
					}

					if (donatorStatus.ValidUntil > DateTime.Now)
					{
						donatorStatus.ValidUntil += key.StatusTime;
					}
					else
					{
						donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
					}
					Utils.Embed.SetTitle($"🎉 Congratulations, {e.Author.Username}")
						.SetColor(226, 46, 68)
						.SetDescription($"You have successfully redeemed a donator key, I've given you **{key.StatusTime.TotalDays}** days of donator status.")
						.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
						.SetThumbnailUrl("https://i.imgur.com/OwwA5fV.png")
						.QueueToChannel(e.Channel);

					context.DonatorKey.Remove(key);
					await context.SaveChangesAsync();
				}
				else
				{
					await e.Channel.SendMessageAsync("invalid key");
				}
			}
		}

		[Command(Name = "box")]
		public async Task BoxAsync(EventContext e)
			=> await PerformCall(e, $"/api/box?text={e.arguments}&url={(await GetUrlFromMessageAsync(e))}");

		[Command(Name = "disability")]
		public async Task DisabilityAsync(EventContext e)
			=> await PerformCall(e, "/api/disability?url=" + (await GetUrlFromMessageAsync(e)));

		[Command(Name = "tohru")]
		public async Task TohruAsync(EventContext e)
			=> await PerformCall(e, "/api/tohru?text=" + e.arguments);

        [Command(Name = "truth")]
        public async Task TruthAsync(EventContext e)
			=> await PerformCall(e, "/api/yagami?text=" + e.arguments);

		[Command(Name = "trapcard")]
		public async Task YugiAsync(EventContext e)
			=> await PerformCall(e, $"/api/yugioh?url={(await GetUrlFromMessageAsync(e))}");

		private async Task<string> GetUrlFromMessageAsync(EventContext e)
		{
			string url = e.Author.AvatarUrl;

			if (e.message.Attachments.Count > 0)
			{
				url = e.message.Attachments.First().Url;
			}

			if (e.message.MentionedUserIds.Count > 0)
			{
				url = (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).AvatarUrl;
			}

			return url;
		}

		private async Task PerformCall(EventContext e, string url)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (u == null)
				{
					SendNotADonatorError(e.Channel);
					return;
				}

				if (await u.IsDonatorAsync(context))
				{
					Stream s = await client.GetStreamAsync(url);
					await e.Channel.SendFileAsync(s, "meme.png");
				}
				else
				{
					SendNotADonatorError(e.Channel);
				}
			}
		}

        private void SendNotADonatorError(IDiscordMessageChannel channel)
        {
            Utils.Embed
                .SetTitle("Sorry!")
                .SetDescription("... but you haven't donated yet, please support us with a small donation to unlock these commands!")
                .AddInlineField("Already donated?", "Make sure to join the Miki Support server and claim your donator status!")
                .AddInlineField("Where do I donate?", "You can find our patreon at https://patreon.com/mikibot")
                .QueueToChannel(channel);
        }
    }
}