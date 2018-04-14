using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using Miki.Modules.Gambling.Managers;
using Discord;
using Miki.Framework.Extension;
using Miki.API;
using System.Collections.Concurrent;
using StackExchange.Redis;
using Miki.Accounts.Achievements;
using Miki.Framework.Languages;
using Miki.Accounts.Achievements.Objects;

namespace Miki.Modules
{
	[Module("Gambling")]
	public class GamblingModule
	{
		// TODO: move to api
		TaskScheduler<string> taskScheduler = new TaskScheduler<string>();
		string lotteryKey = "lottery:tickets";
		RedisDictionary lotteryDict = new RedisDictionary("lottery", Global.RedisClient);
		int lotteryId = 0;

		public GamblingModule()
		{
			if (!Global.Config.IsMainBot)
			{
				lotteryId = taskScheduler.AddTask(0, (s) =>
				{
					long size = Global.RedisClient.Database.ListLength(lotteryKey);

					if (size == 0)
						return;

					string value = Global.RedisClient.Database.ListGetByIndex(lotteryKey, MikiRandom.Next(size));

					ulong winnerId = ulong.Parse(value);
					int wonAmount = (int)Math.Round(size * 100 * 0.75);

					IUser user = Bot.Instance.Client.GetUser(winnerId);

					using (var context = new MikiContext())
					{
						long id = winnerId.ToDbLong();
						User profileUser = context.Users.Find(id);

						if (user != null)
						{
							IMessageChannel channel = user.GetOrCreateDMChannelAsync().Result;

							EmbedBuilder embed = new EmbedBuilder()
							{
								Author = new EmbedAuthorBuilder()
								{
									Name = "Winner winner chicken dinner",
									IconUrl = user.GetAvatarUrl()
								},
								Description = $"Wow! You won the lottery and gained {wonAmount} mekos!"
							};

							profileUser.AddCurrencyAsync(wonAmount, channel);

							embed.Build().QueueToChannel(channel);

							context.SaveChanges();

							Global.RedisClient.Database.KeyDelete(lotteryKey);
							Global.RedisClient.Database.StringSet("lottery:winner", profileUser.Name ?? "unknown");
							lotteryDict.ClearAsync();

							var lotteryAchievement = AchievementManager.Instance.GetContainerById("lottery");

							if (wonAmount > 100000)
							{
								lotteryAchievement.Achievements[0].UnlockAsync(channel, user, 0);
							}

							if (wonAmount > 10000000)
							{
								lotteryAchievement.Achievements[1].UnlockAsync(channel, user, 1);
							}

							if (wonAmount > 250000000)
							{
								lotteryAchievement.Achievements[2].UnlockAsync(channel, user, 1);
							}
						}
					}
				}, "", new TimeSpan(0, 1, 0, 0), true);
			}
		}

		[Command(Name = "rps")]
		public async Task RPSAsync(EventContext e)
		{
			await ValidateBet(e, StartRPS, 10000);
		}

		public async Task StartRPS(EventContext e, int bet)
		{
			float rewardMultiplier = 1f;

			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbed("You need to choose a weapon!")
					.Build().QueueToChannel(e.Channel);
			}
			else
			{
				User user;
				RPSManager rps = RPSManager.Instance;
				EmbedBuilder resultMessage = Utils.Embed
					.WithTitle("Rock, Paper, Scissors!");

				if (rps.TryParse(e.Arguments.Get(1).Argument, out RPSWeapon playerWeapon))
				{
					RPSWeapon botWeapon = rps.GetRandomWeapon();

					resultMessage.WithDescription($"{playerWeapon.Name.ToUpper()} {playerWeapon.Emoji} vs. {botWeapon.Emoji} {botWeapon.Name.ToUpper()}");

					switch (rps.CalculateVictory(playerWeapon, botWeapon))
					{
						case RPSManager.VictoryStatus.WIN:
						{
							using (var context = new MikiContext())
							{
								user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
								if (user != null)
								{
									await user.AddCurrencyAsync((int)(bet * rewardMultiplier), e.Channel);
									await context.SaveChangesAsync();
								}
							}
							resultMessage.Description += $"\n\nYou won `{(int)(bet * rewardMultiplier)}` mekos! Your new balance is `{user.Currency}`.";
						}
						break;

						case RPSManager.VictoryStatus.LOSE:
						{
							using (var context = new MikiContext())
							{
								user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
								if (user != null)
								{
									await user.AddCurrencyAsync(-bet, e.Channel, null);
									await context.SaveChangesAsync();
								}
							}
							resultMessage.Description += $"\n\nYou lost `{bet}` mekos ! Your new balance is `{user.Currency}`.";
						}
						break;

						case RPSManager.VictoryStatus.DRAW:
						{
							resultMessage.Description += $"\n\nIt's a draw! no mekos were lost!.";
						}
						break;
					}
				}
				else
				{
					resultMessage.WithDescription("Invalid weapon!").Build().QueueToChannel(e.Channel);
					return;
				}
				resultMessage.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "blackjack", Aliases = new[] { "bj" })]
		public async Task BlackjackAsync(EventContext e)
		{
			if (EventSystem.Instance.PrivateCommandHandlerExist(e.Author.Id, e.Channel.Id))
			{
				e.ErrorEmbed(e.GetResource("blackjack_error_instance_exists"))
					.Build().QueueToChannel(e.Channel);

				return;
			}

			await ValidateBet(e, StartBlackjack);
		}

        public async Task StartBlackjack(EventContext e, int bet)
        {
            using (var context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				await user.AddCurrencyAsync(-bet, e.Channel);
				await context.SaveChangesAsync();
			}

			BlackjackManager bm = new BlackjackManager();

			IUserMessage message = await bm.CreateEmbed(e)
					.SendToChannel(e.Channel);

			if (bm.Worth(bm.player) == 21)
				await OnBlackjackWin(e, bm, message, bet);

			CommandHandler c = new CommandHandlerBuilder(EventSystem.Instance)
				.AddPrefix("")
				.SetOwner(e.message)
				.AddCommand(
					new CommandEvent("hit")
						.Default(async (ec) => await OnBlackjackHit(ec, bm, message, bet)))
				.AddCommand(
					new CommandEvent("stand")
						.SetAliases("knock", "stay", "stop")
						.Default(async (ec) => await OnBlackjackHold(ec, bm, message, bet)))
				//.AddCommand(
				//	new CommandEvent("double")
				//		.SetAliases("d")
				//		.Default(async (ec) => await OnBlackjackDouble(ec, bm, message, bet)))
				.Build();

			EventSystem.Instance.AddPrivateCommandHandler(e.message, c);

		}

		private async Task OnBlackjackHit(EventContext e, BlackjackManager bm, IUserMessage instanceMessage, int bet)
		{
			IGuildUser me = await e.Guild.GetCurrentUserAsync();

			if (me.GetPermissions(e.Channel as IGuildChannel).Has(ChannelPermission.ManageMessages))
			{
				await e.message.DeleteAsync();
			}

			bm.player.AddToHand(bm.deck.DrawRandom());

			if (bm.Worth(bm.player) > 21)
			{
				await OnBlackjackDead(e, bm, instanceMessage, bet);
			}
			else
			{
				if (bm.player.Hand.Count == 5)
				{
					await OnBlackjackHold(e, bm, instanceMessage, bet, true);
					return;
				}
				else if (bm.Worth(bm.player) == 21 && bm.Worth(bm.dealer) != 21)
				{
					await OnBlackjackWin(e, bm, instanceMessage, bet);
					return;
				}
				else if (bm.Worth(bm.dealer) == 21 && bm.Worth(bm.player) != 21)
				{
					await OnBlackjackDead(e, bm, instanceMessage, bet);
					return;
				}

				await instanceMessage.ModifyAsync(x =>
				{
					x.Embed = bm.CreateEmbed(e);
				});
			}
		}

		private async Task OnBlackjackHold(EventContext e, BlackjackManager bm, IUserMessage instanceMessage,
			int bet, bool charlie = false)
		{
			bm.dealer.Hand.ForEach(x => x.isPublic = true);

			if (!charlie)
			{
				IGuildUser me = await e.Guild.GetCurrentUserAsync();

				if (me.GetPermissions(e.Channel as IGuildChannel).Has(ChannelPermission.ManageMessages))
				{
					await e.message.DeleteAsync();
				}
			}

			while (true)
			{
				if (bm.Worth(bm.dealer) >= Math.Max(bm.Worth(bm.player), 17))
				{
					if (charlie)
					{
						if (bm.dealer.Hand.Count == 5)
						{
							if (bm.Worth(bm.dealer) == bm.Worth(bm.player))
							{
								await OnBlackjackDraw(e, bm, instanceMessage, bet);
								return;
							}
							await OnBlackjackDead(e, bm, instanceMessage, bet);
							return;
						}
					}
					else
					{
						await OnBlackjackDead(e, bm, instanceMessage, bet);
						return;
					}
				}

				bm.dealer.AddToHand(bm.deck.DrawRandom());

				if (bm.Worth(bm.dealer) > 21)
				{
					await OnBlackjackWin(e, bm, instanceMessage, bet);
					return;
				}
			}
		}

		private async Task OnBlackjackDraw(EventContext e, BlackjackManager bm, IUserMessage instanceMessage,
			int bet)
		{
			await e.commandHandler.RequestDisposeAsync();

			User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (user != null)
				{
					await user.AddCurrencyAsync(bet, e.Channel);
					await context.SaveChangesAsync();
				}
			}

			await instanceMessage.ModifyAsync(x =>
			{
				x.Embed = bm.CreateEmbed(e).ToEmbedBuilder()
			   .SetAuthor(e.GetResource("miki_blackjack_draw_title") + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot")
			   .WithDescription(e.GetResource("blackjack_draw_description") + "\n" +
							   e.GetResource("miki_blackjack_current_balance", user.Currency)).Build();
			});
		}

		private async Task OnBlackjackDead(EventContext e, BlackjackManager bm, IUserMessage instanceMessage,
			int bet)
		{
			await e.commandHandler.RequestDisposeAsync();

			User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
			}

			await instanceMessage.ModifyAsync(async x =>
			{
				x.Embed = bm.CreateEmbed(e).ToEmbedBuilder()
					.SetAuthor(e.GetResource("miki_blackjack_lose_title") + " | " + e.Author.Username, (await e.Guild.GetCurrentUserAsync()).GetAvatarUrl(), "https://patreon.com/mikibot")
					.WithDescription(e.GetResource("miki_blackjack_lose_description") + "\n" + e.GetResource("miki_blackjack_new_balance", user.Currency)).Build();
			});
		}

		private async Task OnBlackjackWin(EventContext e, BlackjackManager bm, IUserMessage instanceMessage, int bet)
		{
			await e.commandHandler.RequestDisposeAsync();

			User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (user != null)
				{
					await user.AddCurrencyAsync(bet * 2, e.Channel);
					await context.SaveChangesAsync();
				}
			}

			await instanceMessage.ModifyAsync(x =>
			{
				x.Embed = bm.CreateEmbed(e).ToEmbedBuilder()
					.SetAuthor(e.GetResource("miki_blackjack_win_title") + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot")
					.WithDescription(e.GetResource("miki_blackjack_win_description", bet * 2) + "\n" + e.GetResource("miki_blackjack_new_balance", user.Currency))
					.Build();
			});
		}

		[Command(Name = "flip")]
		public async Task FlipAsync(EventContext e)
		{
			await ValidateBet(e, StartFlip, 10000);
		}

		private async Task StartFlip(EventContext e, int bet)
		{
			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbed("Please pick either `heads` or `tails`!")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			string sideParam = e.Arguments.Get(1).Argument.ToLower();

			if (bet <= 0)
			{
				// TODO: add a error message
				return;
			}

			int pickedSide = -1;

			if (sideParam[0] == 'h')
			{
				pickedSide = 1;
			}
			else if (sideParam[0] == 't')
			{
				pickedSide = 0;
			}

			if (pickedSide == -1)
			{
				e.ErrorEmbed("This is not a valid option!")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			string headsUrl = "https://miki.ai/assets/img/miki-default-heads.png";
			string tailsUrl = "https://miki.ai/assets/img/miki-default-tails.png";

			if (e.Arguments.Contains("-bonus"))
			{
				headsUrl = "https://miki.ai/assets/img/miki-secret-heads.png";
				tailsUrl = "https://miki.ai/assets/img/miki-secret-tails.png";
			}

			int side = MikiRandom.Next(2);
			string imageUrl = side == 1 ? headsUrl : tailsUrl;

			bool win = (side == pickedSide);
			int currencyNow = 0;

			using (MikiContext context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (!win)
					bet = -bet;
				u.Currency += bet;
				currencyNow = u.Currency;
				await context.SaveChangesAsync();
			}

			string output = "";

			if (win)
			{
				output = e.GetResource("flip_description_win", $"`{bet}`");
			}
			else
			{
				output = e.GetResource("flip_description_lose");
			}

			output += "\n" + e.GetResource("miki_blackjack_new_balance", currencyNow);

			Embed embed = Utils.Embed
				.SetAuthor(e.GetResource("flip_header") + " | " + e.Author.Username, e.Author.GetAvatarUrl(),
					"https://patreon.com/mikibot")
				.WithDescription(output)
				.WithThumbnailUrl(imageUrl).Build();

			embed.QueueToChannel(e.Channel);
		}

		[Command(Name = "slots", Aliases = new[] { "s" })]
		public async Task SlotsAsync(EventContext e)
		{
			await ValidateBet(e, StartSlots, 99999);
		}

		public async Task StartSlots(EventContext e, int bet)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				int moneyReturned = 0;

				string[] objects =
				{
					"🍒", "🍒", "🍒", "🍒",
					"🍊", "🍊", "🍊",
					"🍓", "🍓",
					"🍍", "🍍",
					"🍇", "🍇",
					"🍉", "🍉",
					"⭐", "⭐",
					"🍉",
					"🍍", "🍍",
					"🍓", "🍓",
					"🍊", "🍊", "🍊",
					"🍒", "🍒", "🍒", "🍒",
				};

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(e.GetResource(LocaleTags.SlotsHeader) + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot");

				string[] objectsChosen =
				{
					objects[MikiRandom.Next(objects.Length)],
					objects[MikiRandom.Next(objects.Length)],
					objects[MikiRandom.Next(objects.Length)]
				};

				Dictionary<string, int> score = new Dictionary<string, int>();

				foreach (string o in objectsChosen)
				{
					if (score.ContainsKey(o))
					{
						score[o]++;
						continue;
					}
					score.Add(o, 1);
				}

				if (score.ContainsKey("🍒"))
				{
					if (score["🍒"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 0.5f);
					}
					else if (score["🍒"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1f);
					}
				}
				if (score.ContainsKey("🍊"))
				{
					if (score["🍊"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 0.8f);
					}
					else if (score["🍊"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1.5f);
					}
				}
				if (score.ContainsKey("🍓"))
				{
					if (score["🍓"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1f);
					}
					else if (score["🍓"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 2f);
					}
				}
				if (score.ContainsKey("🍍"))
				{
					if (score["🍍"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1f);
					}
					if (score["🍍"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 4f);
					}
				}
				if (score.ContainsKey("🍇"))
				{
					if (score["🍇"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1.2f);
					}
					if (score["🍇"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 6f);
					}
				}
				if (score.ContainsKey("🍉"))
				{
					if (score["🍉"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 1.5f);
					}
					if (score["🍉"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 10f);
					}
				}
				if (score.ContainsKey("⭐"))
				{
					if (score["⭐"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 2f);
					}
					if (score["⭐"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 12f);

						await AchievementManager.Instance.GetContainerById("slots").CheckAsync(new BasePacket()
						{
							discordChannel = e.Channel,
							discordUser = e.Author
						});
					}
				}

				if (moneyReturned == 0)
				{
					moneyReturned = -bet;
					embed.AddField(e.GetResource("miki_module_fun_slots_lose_header"),
						e.GetResource("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
				}
				else
				{
					embed.AddField(e.GetResource(LocaleTags.SlotsWinHeader),
						e.GetResource(LocaleTags.SlotsWinMessage, moneyReturned, u.Currency + moneyReturned));
				}

				embed.Description = string.Join(" ", objectsChosen);
				await u.AddCurrencyAsync(moneyReturned, e.Channel);
				await context.SaveChangesAsync();

				embed.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "lottery")]
		public async Task LotteryAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
			{
				long totalTickets = await Global.RedisClient.Database.ListLengthAsync(lotteryKey);
				long yourTickets = 0;

				string latestWinner = Global.RedisClient.Database.StringGet("lottery:winner");

				if (await lotteryDict.ContainsAsync(e.Author.Id))
				{
					yourTickets = long.Parse(await lotteryDict.GetAsync(e.Author.Id));
				}

				new EmbedBuilder()
				{
					Title = "🍀 Lottery",
					Description = "Make the biggest gamble, and get paid off massively if legit.",
					Color = new Color(119, 178, 85)
				}.AddInlineField("Tickets Owned", yourTickets)
				.AddInlineField("Drawing In", taskScheduler.GetInstance(0, lotteryId).TimeLeft.ToTimeString(e.Channel.Id, true))
				.AddInlineField("Total Tickets", totalTickets)
				.AddInlineField("Ticket price", $"{100} mekos")
				.AddInlineField("Latest Winner", latestWinner ?? "???")
				.AddInlineField("How to buy?", ">lottery buy [amount]")
				.Build().QueueToChannel(e.Channel);
				return;
			}

			switch (arg.Argument.ToLower())
			{
				case "buy":
				{
					arg = arg.Next();
					int amount = arg?.AsInt(1) ?? 1;

					if (amount < 1)
						amount = 1;

					using (var context = new MikiContext())
					{
						User u = await User.GetAsync(context, e.Author);

						if (amount * 100 > u.Currency)
						{
							e.ErrorEmbedResource("miki_mekos_insufficient")
								.Build().QueueToChannel(e.Channel);
							return;
						}

						await u.AddCurrencyAsync(-amount * 100, e.Channel);

						RedisValue[] tickets = new RedisValue[amount];

						for (int i = 0; i < amount; i++)
						{
							tickets[i] = e.Author.Id.ToString();
						}

						await Global.RedisClient.Database.ListRightPushAsync(lotteryKey, tickets);

						int totalTickets = 0;

						if (await lotteryDict.ContainsAsync(e.Author.Id.ToString()))
						{
							totalTickets = int.Parse(await lotteryDict.GetAsync(e.Author.Id.ToString()));
						}

						await lotteryDict.AddAsync(e.Author.Id, amount + totalTickets);

						await context.SaveChangesAsync();

						Utils.SuccessEmbed(e.Channel.Id, $"Successfully bought {amount} tickets!")
							.QueueToChannel(e.Channel);
					}
				}
				break;
			}
		}

		// TODO: probable rewrite at some point
		public async Task ValidateBet(EventContext e, Func<EventContext, int, Task> callback = null, int maxBet = 1000000)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg != null)
			{
				const int noAskLimit = 10000;

				using (MikiContext context = new MikiContext())
				{
					User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

					if (user == null)
					{
						// TODO: add user null error
						return;
					}

					string checkArg = arg.Argument;

					if (int.TryParse(checkArg, out int bet))
					{

					}
					else if (checkArg.ToLower() == "all" || checkArg == "*")
					{
						bet = user.Currency > maxBet ? maxBet : user.Currency;
					}
					else
					{
						e.ErrorEmbed(e.GetResource("miki_error_gambling_parse_error"))
							.Build().QueueToChannel(e.Channel);
						return;
					}

					if (bet < 1)
					{
						e.ErrorEmbed(e.GetResource("miki_error_gambling_zero_or_less"))
							.Build().QueueToChannel(e.Channel);
					}
					else if (bet > user.Currency)
					{
						e.ErrorEmbed(e.GetResource("miki_mekos_insufficient"))
							.Build().QueueToChannel(e.Channel);
					}
					else if (bet > maxBet)
					{
						e.ErrorEmbed($"you cannot bet more than {maxBet} mekos!")
							.Build().QueueToChannel(e.Channel);
						return;
					}
					else if (bet > noAskLimit)
					{
						EmbedBuilder embed = Utils.Embed;
						embed.Description =
							$"Are you sure you want to bet **{bet}**? You currently have `{user.Currency}` mekos.\n\nType `yes` to confirm.";
						embed.Color = new Color(0.4f, 0.6f, 1f);
						embed.Build().QueueToChannel(e.Channel);

						CommandHandler confirmCommand = new CommandHandlerBuilder(EventSystem.Instance)
							.AddPrefix("")
							.DisposeInSeconds(20)
							.SetOwner(e.message)
							.AddCommand(
								new CommandEvent("yes")
									.Default((ec) => ValidateGlitch(ec, callback, bet)))
									.Build();

						EventSystem.Instance.AddPrivateCommandHandler(e.message, confirmCommand);
					}
					else
					{
						if (callback != null)
						{
							await callback(e, bet);
						}
					}
				}
			}
			else
			{
				e.ErrorEmbed(e.GetResource("miki_error_gambling_no_arg"))
					.Build().QueueToChannel(e.Channel);
			}
		}

		public async Task ValidateGlitch(EventContext e, Func<EventContext, int, Task> callback, int bet)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				await e.commandHandler.RequestDisposeAsync();

				if ((await e.Guild.GetCurrentUserAsync()).GetPermissions(e.Channel as IGuildChannel).ManageMessages)
					await e.message.DeleteAsync();


				if (callback != null)
				{
					if (bet > u.Currency)
					{
						e.ErrorEmbed(e.GetResource("miki_mekos_insufficient"))
							.AddInlineField("Pro tip!", "You can get more daily mekos by voting on us [here!](https://discordbots.org/bot/160105994217586689)")
							.Build().QueueToChannel(e.Channel);
						return;
					}
					await callback(e, bet);
				}
			}
		}
	}
}