using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using Miki.API.Cards.Objects;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Helpers;
using Miki.Localization;
using Miki.Models;
using Miki.Modules.Gambling.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Modules
{
	[Module("Gambling")]
	public class GamblingModule
	{
		[Command(Name = "rps")]
		public async Task RPSAsync(EventContext e)
		{
			int? bet = await ValidateBetAsync(e, 10000);
			if (bet.HasValue)
			{
				await StartRPS(e, bet.Value);
			}
		}

		public async Task StartRPS(EventContext e, int bet)
		{
			float rewardMultiplier = 1f;

			if (e.Arguments.Pack.Length < 2)
			{
				await e.ErrorEmbed("You need to choose a weapon!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
			}
			else
			{
				User user;
				RPSManager rps = RPSManager.Instance;
				EmbedBuilder resultMessage = new EmbedBuilder()
					.SetTitle("Rock, Paper, Scissors!");

                e.Arguments.Take(out string weapon);

				if (rps.TryParse(weapon, out RPSWeapon playerWeapon))
				{
					RPSWeapon botWeapon = rps.GetRandomWeapon();

					resultMessage.SetDescription($"{playerWeapon.Name.ToUpper()} {playerWeapon.Emoji} vs. {botWeapon.Emoji} {botWeapon.Name.ToUpper()}");

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
									user.RemoveCurrency(bet);
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
					await resultMessage.SetDescription("Invalid weapon!").ToEmbed()
						.QueueToChannelAsync(e.Channel);
					return;
				}
                await resultMessage.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}

		[Command(Name = "blackjack", Aliases = new[] { "bj" })]
		public async Task BlackjackAsync(EventContext e)
		{
            e.Arguments.Take(out string subCommand);

			switch (subCommand)
			{
				case "new":
				{
					await OnBlackjackNew(e);
				}
				break;

				case "hit":
				case "draw":
				{
					await OnBlackjackHitAsync(e);
				}
				break;

				case "stay":
				case "stand":
				{
					await OnBlackjackHold(e);
				}
				break;

				default:
				{
                    await new EmbedBuilder()
						.SetTitle("🎲 Blackjack")
						.SetColor(234, 89, 110)
						.SetDescription("Play a game of blackjack against miki!\n\n" +
							"`>blackjack new <bet> [ok]` to start a new game\n" +
							"`>blackjack hit` to draw a card\n" +
							"`>blackjack stay` to stand")
						.ToEmbed()
						.QueueToChannelAsync(e.Channel);
				}
				break;
			}
		}

		public async Task OnBlackjackNew(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
			int? bet = await ValidateBetAsync(e);

			if (bet.HasValue)
			{
				if (await cache.ExistsAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}"))
				{
                    await e.ErrorEmbedResource(new LanguageResource("blackjack_session_exists"))
						.ToEmbed().QueueToChannelAsync(e.Channel);
					return;
				}

				using (var context = new MikiContext())
				{
					var user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

					if (user == null)
						return;

					user.RemoveCurrency(bet.Value);

					await context.SaveChangesAsync();
				}

				BlackjackManager manager = new BlackjackManager(bet.Value);

				CardHand dealer = manager.AddPlayer(0);
				manager.AddPlayer(e.Author.Id);

				manager.DealAll();
				manager.DealAll();

				dealer.Hand[1].isPublic = false;

				IDiscordMessage message = await manager.CreateEmbed(e)
					.ToEmbed().SendToChannel(e.Channel);

				manager.MessageId = message.Id;

				await cache.UpsertAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}", manager.ToContext(), TimeSpan.FromHours(24));
			}
		}

		private async Task OnBlackjackHitAsync(EventContext e)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var api = (IApiClient)e.Services.GetService(typeof(IApiClient));

            BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(cache, e.Channel.Id, e.Author.Id);

			CardHand Player = bm.GetPlayer(e.Author.Id);
			CardHand Dealer = bm.GetPlayer(0);

			bm.DealTo(Player);

			if (bm.Worth(Player) > 21)
			{
				await OnBlackjackDead(e, bm);
			}
			else
			{
				if (Player.Hand.Count == 5)
				{
					await OnBlackjackHold(e, true);
					return;
				}
				else if (bm.Worth(Player) == 21 && bm.Worth(Dealer) != 21)
				{
					await OnBlackjackWin(e, bm);
					return;
				}
				else if (bm.Worth(Dealer) == 21 && bm.Worth(Player) != 21)
				{
					await OnBlackjackDead(e, bm);
					return;
				}

				await api.EditMessageAsync(e.Channel.Id, bm.MessageId, new EditMessageArgs
				{
					embed = bm.CreateEmbed(e).ToEmbed()
				});

				await cache.UpsertAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}", bm.ToContext(), TimeSpan.FromHours(24));
			}
		}

		private async Task OnBlackjackHold(EventContext e, bool charlie = false)
        {
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(cache, e.Channel.Id, e.Author.Id);

			CardHand Player = bm.GetPlayer(e.Author.Id);
			CardHand Dealer = bm.GetPlayer(0);

			Dealer.Hand.ForEach(x => x.isPublic = true);

			while (true)
			{
				if (bm.Worth(Dealer) >= Math.Max(bm.Worth(Player), 17))
				{
					if (charlie)
					{
						if (Dealer.Hand.Count == 5)
						{
							if (bm.Worth(Dealer) == bm.Worth(Player))
							{
								await OnBlackjackDraw(e, bm);
								return;
							}
							await OnBlackjackDead(e, bm);
							return;
						}
					}
					else
					{
						if (bm.Worth(Dealer) == bm.Worth(Player))
						{
							await OnBlackjackDraw(e, bm);
							return;
						}
						await OnBlackjackDead(e, bm);
						return;
					}
				}

				bm.DealTo(Dealer);

				if (bm.Worth(Dealer) > 21)
				{
					await OnBlackjackWin(e, bm);
					return;
				}
			}
		}

		private async Task OnBlackjackDraw(EventContext e, BlackjackManager bm)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var api = (IApiClient)e.Services.GetService(typeof(IApiClient));

            User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (user != null)
				{
					await user.AddCurrencyAsync(bm.Bet, e.Channel);
					await context.SaveChangesAsync();
				}
			}

			await api.EditMessageAsync(e.Channel.Id, bm.MessageId,
				new EditMessageArgs
				{
					embed = bm.CreateEmbed(e)
				   .SetAuthor(
						e.Locale.GetString("blackjack_draw_title") + " | " + e.Author.Username,
						e.Author.GetAvatarUrl(),
						"https://patreon.com/mikibot"
					)
				   .SetDescription(
						e.Locale.GetString("blackjack_draw_description") + "\n" +
						e.Locale.GetString("miki_blackjack_current_balance", user.Currency)
					).ToEmbed()
				}
			);

			await cache.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");
		}

		private async Task OnBlackjackDead(EventContext e, BlackjackManager bm)
		{
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var api = (IApiClient)e.Services.GetService(typeof(IApiClient));

            User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
			}

            await cache.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");

            await api.EditMessageAsync(e.Channel.Id, bm.MessageId,
				new EditMessageArgs
				{
					embed = bm.CreateEmbed(e)
							.SetAuthor(
								e.Locale.GetString("miki_blackjack_lose_title") + " | " + e.Author.Username,
								(await e.Guild.GetSelfAsync()).GetAvatarUrl(), "https://patreon.com/mikibot"
							)
							.SetDescription(e.Locale.GetString("miki_blackjack_lose_description") + "\n" + e.Locale.GetString("miki_blackjack_new_balance", user.Currency)
							).ToEmbed()
				});

		}

        private async Task OnBlackjackWin(EventContext e, BlackjackManager bm)
        {
            var cache = (ICacheClient)e.Services.GetService(typeof(ICacheClient));
            var api = (IApiClient)e.Services.GetService(typeof(IApiClient));

            await cache.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");

            User user;
            using (var context = new MikiContext())
            {
                user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (user != null)
                {
                    await user.AddCurrencyAsync(bm.Bet * 2, e.Channel);

                    await api.EditMessageAsync(e.Channel.Id, bm.MessageId, new EditMessageArgs
                    {
                        embed = bm.CreateEmbed(e)
                        .SetAuthor(e.Locale.GetString("miki_blackjack_win_title") + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot")
                        .SetDescription(e.Locale.GetString("miki_blackjack_win_description", bm.Bet * 2) + "\n" + e.Locale.GetString("miki_blackjack_new_balance", user.Currency))
                        .ToEmbed()
                    });

                    await context.SaveChangesAsync();
                }
            }
        }

		[Command(Name = "flip")]
		public async Task FlipAsync(EventContext e)
		{
			int? bet = await ValidateBetAsync(e, 10000);
			if (bet.HasValue)
			{
				await StartFlip(e, bet.Value);
			}
		}

		private async Task StartFlip(EventContext e, int bet)
		{
			if (e.Arguments.Pack.Length < 2)
			{
                await e.ErrorEmbed("Please pick either `heads` or `tails`!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

            e.Arguments.Take(out string sideParam);

			int pickedSide = -1;

			if (char.ToLower(sideParam[0]) == 'h')
			{
				pickedSide = 1;
			}
			else if (char.ToLower(sideParam[0]) == 't')
			{
				pickedSide = 0;
			}

			if (pickedSide == -1)
			{
                await e.ErrorEmbed("This is not a valid option!")
					.ToEmbed().QueueToChannelAsync(e.Channel);
				return;
			}

			string headsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-heads.png";
			string tailsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-tails.png";

            if (e.Arguments.Peek(out string bonus))
            {
                if (bonus == "-bonus")
                {
                    headsUrl = "https://cdn.miki.ai/commands/miki-secret-heads.png";
                    tailsUrl = "https://cdn.miki.ai/commands/miki-secret-tails.png";
                }
            }

			int side = MikiRandom.Next(2);
			string imageUrl = side == 1 ? headsUrl : tailsUrl;

			bool win = (side == pickedSide);
			int currencyNow = 0;

			using (MikiContext context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (!win)
				{
					u.RemoveCurrency(bet);
				}
				else
				{
					await u.AddCurrencyAsync(bet);
				}

				currencyNow = u.Currency;

				await context.SaveChangesAsync();
			}

			string output;
			if (win)
			{
				output = e.Locale.GetString("flip_description_win", $"`{bet}`");
			}
			else
			{
				output = e.Locale.GetString("flip_description_lose");
			}

			output += "\n" + e.Locale.GetString("miki_blackjack_new_balance", currencyNow);

			DiscordEmbed embed = new EmbedBuilder()
				.SetAuthor(e.Locale.GetString("flip_header") + " | " + e.Author.Username, e.Author.GetAvatarUrl(),
					"https://patreon.com/mikibot")
				.SetDescription(output)
				.SetThumbnail(imageUrl)
				.ToEmbed();

            await embed.QueueToChannelAsync(e.Channel);
		}

		[Command(Name = "slots", Aliases = new[] { "s" })]
		public async Task SlotsAsync(EventContext e)
		{
			int? i = await ValidateBetAsync(e, 99999);
			if (i.HasValue)
			{
				await StartSlots(e, i.Value);
			}
		}

		public async Task StartSlots(EventContext e, int bet)
		{
			using (var context = new MikiContext())
			{
				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				int moneyReturned = 0;

				string[] objects =
				{
					"🍒", "🍒", "🍒", "🍒", "🍒", "🍒", "🍒",
					"🍊", "🍊", "🍊", "🍊", "🍊", "🍊",
					"🍓", "🍓", "🍓", "🍓", "🍓",
					"🍍", "🍍", "🍍", "🍍",
					"🍇", "🍇", "🍇",
					"🍉", "🍉",
					"⭐",
				};

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor(e.Locale.GetString("miki_module_fun_slots_header") + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot");

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
						moneyReturned = (int)Math.Ceiling(bet * 0.25f);
					}
					else if (score["🍒"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 3f);
					}
				}
				if (score.ContainsKey("🍊"))
				{
					if (score["🍊"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 0.5f);
					}
					else if (score["🍊"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 5f);
					}
				}
				if (score.ContainsKey("🍓"))
				{
					if (score["🍓"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 0.75f);
					}
					else if (score["🍓"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 7f);
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
						moneyReturned = (int)Math.Ceiling(bet * 10f);
					}
				}
				if (score.ContainsKey("🍇"))
				{
					if (score["🍇"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 2f);
					}
					if (score["🍇"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 15f);
					}
				}
				if (score.ContainsKey("🍉"))
				{
					if (score["🍉"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 3f);
					}
					if (score["🍉"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 25f);
					}
				}
				if (score.ContainsKey("⭐"))
				{
					if (score["⭐"] == 2)
					{
						moneyReturned = (int)Math.Ceiling(bet * 7f);
					}
					if (score["⭐"] == 3)
					{
						moneyReturned = (int)Math.Ceiling(bet * 75f);

						await AchievementManager.Instance.GetContainerById("slots").CheckAsync(new BasePacket()
						{
							discordChannel = e.Channel,
							discordUser = e.Author
						});
					}
				}

				if (moneyReturned == 0)
				{
					embed.AddField(e.Locale.GetString("miki_module_fun_slots_lose_header"),
						e.Locale.GetString("miki_module_fun_slots_lose_amount", bet, u.Currency - bet));
					u.RemoveCurrency(bet);
				}
				else
				{
					embed.AddField(e.Locale.GetString("miki_module_fun_slots_win_header"),
						e.Locale.GetString("miki_module_fun_slots_win_amount", moneyReturned, u.Currency + moneyReturned));
					await u.AddCurrencyAsync(moneyReturned, e.Channel);
				}

				embed.Description = string.Join(" ", objectsChosen);
				await context.SaveChangesAsync();

                await embed.ToEmbed().QueueToChannelAsync(e.Channel);
			}
		}



        //[Command(Name = "lottery")]
        //public async Task LotteryAsync(EventContext e)
        //{
        //	ArgObject arg = e.Arguments.FirstOrDefault();

        //	if (arg == null)
        //	{
        //		long totalTickets = await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListLengthAsync(lotteryKey);
        //		long yourTickets = 0;

        //		string latestWinner = (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).StringGet("lottery:winner");

        //		if (await lotteryDict.ContainsAsync(e.Author.Id))
        //		{
        //			yourTickets = long.Parse(await lotteryDict.GetAsync(e.Author.Id));
        //		}

        //		string timeLeft = taskScheduler?.GetInstance(0, lotteryId).TimeLeft.ToTimeString(e.Locale, true) ?? "1h?m?s - will be fixed soon!";

        //		new EmbedBuilder()
        //		{
        //			Title = "🍀 Lottery",
        //			Description = "Make the biggest gamble, and get paid off massively if legit.",
        //			Color = new Color(119, 178, 85)
        //		}.AddInlineField("Tickets Owned", yourTickets.ToString())
        //		.AddInlineField("Drawing In", timeLeft)
        //		.AddInlineField("Total Tickets", totalTickets.ToString())
        //		.AddInlineField("Ticket price", $"{100} mekos")
        //		.AddInlineField("Latest Winner", latestWinner ?? "no name")
        //		.AddInlineField("How to buy?", ">lottery buy [amount]")
        //		.ToEmbed().QueueToChannelAsync(e.Channel);
        //		return;
        //	}

        //	switch (arg.Argument.ToLower())
        //	{
        //		case "buy":
        //		{
        //			arg = arg.Next();
        //			int amount = arg?.AsInt() ?? 1;

        //			if (amount < 1)
        //				amount = 1;

        //			using (var context = new MikiContext())
        //			{
        //				User u = await DatabaseHelpers.GetUserAsync(context, e.Author);

        //				if (amount * 100 > u.Currency)
        //				{
        //					e.ErrorEmbedResource("miki_mekos_insufficient")
        //						.ToEmbed().QueueToChannelAsync(e.Channel);
        //					return;
        //				}

        //				await u.AddCurrencyAsync(-amount * 100, e.Channel);

        //				RedisValue[] tickets = new RedisValue[amount];

        //				for (int i = 0; i < amount; i++)
        //				{
        //					tickets[i] = e.Author.Id.ToString();
        //				}

        //				await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListRightPushAsync(lotteryKey, tickets);

        //				int totalTickets = 0;

        //				if (await lotteryDict.ContainsAsync(e.Author.Id.ToString()))
        //				{
        //					totalTickets = int.Parse(await lotteryDict.GetAsync(e.Author.Id.ToString()));
        //				}

        //				await lotteryDict.AddAsync(e.Author.Id, amount + totalTickets);

        //				await context.SaveChangesAsync();

        //				e.SuccessEmbed($"Successfully bought {amount} tickets!")
        //					.QueueToChannelAsync(e.Channel);
        //			}
        //		}
        //		break;
        //	}
        //}

        public async Task<int?> ValidateBetAsync(EventContext e, int maxBet = 1000000)
        {
            using (MikiContext context = new MikiContext())
            {
                User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                if (user == null)
                {
                    throw new UserNullException();
                }

                if (e.Arguments.Take(out int bet))
                {
                }
                else if (e.Arguments.Take(out string arg))
                {
                    if (arg.ToLower() == "all" || arg == "*")
                    {
                        bet = Math.Min(user.Currency, maxBet);
                    }
                }

                if (bet == 0)
                {
                    await e.ErrorEmbed(e.Locale.GetString("miki_error_gambling_parse_error"))
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return null;
                }

                if (bet < 0)
                {
                    throw new ArgumentLessThanZeroException();
                }

                if (bet > user.Currency)
                {
                    throw new InsufficientCurrencyException(user.Currency, bet);
                }

                if (bet > maxBet)
                {
                    await e.ErrorEmbed($"you cannot bet more than {maxBet:n0} mekos!")
                        .ToEmbed().QueueToChannelAsync(e.Channel);
                    return null;
                }

                return bet;
            }
		}
	}
}
