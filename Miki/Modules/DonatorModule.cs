using Miki.Framework;
using Miki.Framework.Events.Attributes;
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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Miki.Accounts.Achievements;
using Discord;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using StatsdClient;

namespace Miki.Modules
{
	public class KofiObject
	{
		[JsonProperty("message_id")]
		public Guid MessageId;

		[JsonProperty("timestamp")]
		public DateTimeOffset Timestamp;

		[JsonProperty("type")]
		public string Type;

		[JsonProperty("from_name")]
		public string FromName;

		[JsonProperty("message")]
		public string Message;

		[JsonProperty("amount")]
		public double Amount;

		[JsonProperty("url")]
		public string Url;
	}

	public class DblVoteObject
	{
		[JsonProperty("bot")]
		public ulong BotId;

		[JsonProperty("user")]
		public ulong UserId;

		[JsonProperty("type")]
		public string Type;
	}

	public class PatreonPledgeObject
	{
		[JsonProperty("user_id")]
		public ulong UserId;

		[JsonProperty("keys_rewarded")]
		public int KeysRewarded;
	}

    [Module(Name = "Donator")]
    internal class DonatorModule
    {
		RestClient client = new RestClient(Global.Config.ImageApiUrl)
			.AddHeader("Authorization", Global.Config.MikiApiKey);

		public DonatorModule()
		{
			WebhookManager.OnEvent += async (value) =>
			{
				if(value.auth_code == "DBL_VOTE")
				{
					using (var context = new MikiContext())
					{
						DblVoteObject voteObject = JsonConvert.DeserializeObject<DblVoteObject>(value.data);

						if (voteObject.Type == "upvote")
						{
							IUser user = Bot.Instance.Client.GetUser(voteObject.UserId);

							if (user == null)
								return;

							User u = await User.GetAsync(context, user);

							if (!await Global.redisClient.ExistsAsync($"dbl:vote:{voteObject.UserId}"))
							{
								u.DblVotes++;
								await Global.redisClient.AddAsync($"dbl:vote:{voteObject.UserId}", 1, new TimeSpan(1, 0, 0, 0));

								int addedCurrency = 100 * ((await u.IsDonatorAsync(context)) ? 2 : 1);

								u.Currency += addedCurrency;

								await context.SaveChangesAsync();

								DogStatsd.Increment("votes.dbl");

								new EmbedBuilder()
								{
									Title = "Thanks for voting!",
									Description = ($"We've given you {addedCurrency} mekos to your profile"),
									Color = new Color(64, 255, 64)
								}.Build().QueueToUser(user);
							}

							var achievements = AchievementManager.Instance.GetContainerById("voter");

							switch (u.DblVotes)
							{
								case 1:
								{
									await achievements.Achievements[0].UnlockAsync(user);
								} break;
								case 25:
								{
									await achievements.Achievements[1].UnlockAsync(user, 1);
								} break;
								case 200:
								{
									await achievements.Achievements[2].UnlockAsync(user, 2);
								} break;
							}
						}
					}
				}

				if(value.auth_code == "KOFI_DONATE")
				{
					JObject data = JsonConvert.DeserializeObject<JObject>(value.data);
					KofiObject kofi = JsonConvert.DeserializeObject<KofiObject>(data.GetValue("data").ToObject<string>());

					if (ulong.TryParse(kofi.Message.Split(' ').Last(), out ulong uid))
					{
						IUser user = Bot.Instance.Client.GetUser(uid);

						List<string> allKeys = new List<string>();

						for (int i = 0; i < kofi.Amount / 3; i++)
						{
							allKeys.Add(DonatorKey.GenerateNew().Key.ToString());
						}

						new EmbedBuilder()
						{
							Title = "You donated through ko-fi!",
							Description = "I work hard for miki's quality, thank you for helping me keep the bot running!"
						}.AddField("- Veld#0001", "Here are your key(s)!\n\n`" + string.Join("\n", allKeys) + "`")
						.AddField("How to redeem this key?", $"use this command `>redeemkey`")
						.Build().QueueToUser(user);
					}
				}

				if(value.auth_code == "PATREON_PLEDGES")
				{
					List<PatreonPledgeObject> pledgeObjects = JsonConvert.DeserializeObject<List<PatreonPledgeObject>>(value.data);

					foreach (PatreonPledgeObject pledge in pledgeObjects)
					{
						IUser user = Bot.Instance.Client.GetUser(pledge.UserId);

						EmbedBuilder embed = new EmbedBuilder()
						{
							Title = "You donation came through patreon!",
							Description = "I work hard for miki's quality, thank you for helping me keep the bot running! - Veld#0001"
						};

						int max_per_embed = 20;

						for (int i = 0; i < Math.Ceiling((double)pledge.KeysRewarded / max_per_embed); i++)
						{
							List<string> allKeys = new List<string>();

							for (int j = i * max_per_embed; j < Math.Min(i * max_per_embed + max_per_embed, pledge.KeysRewarded); j++)
							{
								allKeys.Add(DonatorKey.GenerateNew().Key.ToString());
							}

							embed.AddInlineField("Here are your key(s)!", $"`{string.Join("\n", allKeys)}`");
						}

						embed.AddField("How to redeem this key?", $"use this command `>redeemkey`", false)
							.Build().QueueToUser(user);

					}
				}

				if(value.auth_code == "PATREON_DELETE")
				{
					PatreonPledge p = JsonConvert.DeserializeObject<PatreonPledge>(value.data);
					if(ulong.TryParse(p.Included[0].attributes.ToObject<UserAttribute>().DiscordUserId, out ulong s))
					{
						new EmbedBuilder()
						{
							Title = "Sad to see you leave!",
							Description = "However, I won't hold it against you, thank you for your timely support and I hope you'll happily continue using Miki"
						}.Build().QueueToUser(Bot.Instance.Client.GetUser(s));
					}
				}

				if (value.auth_code == "PATREON_CREATE")
				{
					PatreonPledge p = JsonConvert.DeserializeObject<PatreonPledge>(value.data);
					if (ulong.TryParse(p.Included[0].attributes.ToObject<UserAttribute>().DiscordUserId, out ulong s))
					{
						new EmbedBuilder() {
							Title = "Welcome to the family",
							Description = ("In maximal 24 hours you will receive another DM with key(s) depending on your patron amount. (5$/key). Thank you for your support!")
						}.Build().QueueToUser(Bot.Instance.Client.GetUser(s));
					}
				}
			};
		}

		[Command(Name = "redeemkey")]
		public async Task RedeemKeyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;
				Guid guid = Guid.Parse(e.Arguments.Join().Argument);
				DonatorKey key = await context.DonatorKey.FindAsync(guid);
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

					donatorStatus.KeysRedeemed++;

					if (donatorStatus.ValidUntil > DateTime.Now)
					{
						donatorStatus.ValidUntil += key.StatusTime;
					}
					else
					{
						donatorStatus.ValidUntil = DateTime.Now + key.StatusTime;
					}
					
					new EmbedBuilder() {
						Title=($"🎉 Congratulations, {e.Author.Username}"),
						Color=new Color(226, 46, 68),
						Description=($"You have successfully redeemed a donator key, I've given you **{key.StatusTime.TotalDays}** days of donator status."),
						ThumbnailUrl=("https://i.imgur.com/OwwA5fV.png")
					}.AddInlineField("When does my status expire?", donatorStatus.ValidUntil.ToLongDateString())
					.Build().QueueToChannel(e.Channel);

					context.DonatorKey.Remove(key);
					await context.SaveChangesAsync();

					// cheap hack.

					var achievements = AchievementManager.Instance.GetContainerById("donator");

					if(donatorStatus.KeysRedeemed == 1)
					{
						await achievements.Achievements[0].UnlockAsync(e.Channel, e.Author, 0);
					}
					else if (donatorStatus.KeysRedeemed == 5)
					{
						await achievements.Achievements[1].UnlockAsync(e.Channel, e.Author, 1);
					}
					else if (donatorStatus.KeysRedeemed == 25)
					{
						await achievements.Achievements[2].UnlockAsync(e.Channel, e.Author, 2);
					}
				}
				else
				{
					e.ErrorEmbed("Your donation key is invalid!");
				}
			}
		}

		[Command(Name = "box")]
		public async Task BoxAsync(EventContext e)
			=> await PerformCall(e, $"/api/box?text={e.Arguments.Join().Argument}&url={(await GetUrlFromMessageAsync(e))}");

		[Command(Name = "disability")]
		public async Task DisabilityAsync(EventContext e)
			=> await PerformCall(e, "/api/disability?url=" + (await GetUrlFromMessageAsync(e)));

		[Command(Name = "tohru")]
		public async Task TohruAsync(EventContext e)
			=> await PerformCall(e, "/api/tohru?text=" + e.Arguments.Join().Argument);

        [Command(Name = "truth")]
        public async Task TruthAsync(EventContext e)
			=> await PerformCall(e, "/api/yagami?text=" + e.Arguments.Join().Argument);

		[Command(Name = "trapcard")]
		public async Task YugiAsync(EventContext e)
			=> await PerformCall(e, $"/api/yugioh?url={(await GetUrlFromMessageAsync(e))}");

		private async Task<string> GetUrlFromMessageAsync(EventContext e)
		{
			string url = e.Author.GetAvatarUrl();

			if (e.message.Attachments.Count > 0)
			{
				url = e.message.Attachments.First().Url;
			}

			if (e.message.MentionedUserIds.Count > 0)
			{
				url = (await e.Guild.GetUserAsync(e.message.MentionedUserIds.First())).GetAvatarUrl();
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

        private void SendNotADonatorError(IMessageChannel channel)
        {
			new EmbedBuilder()
			{
				Title = "Sorry!",
				Description = "... but you haven't donated yet, please support us with a small donation to unlock these commands!",
			}.AddField("Already donated?", "Make sure to join the Miki Support server and claim your donator status!")
			 .AddField("Where do I donate?", "You can find our patreon at https://patreon.com/mikibot")
			 .Build().QueueToChannel(channel);
        }
    }
}