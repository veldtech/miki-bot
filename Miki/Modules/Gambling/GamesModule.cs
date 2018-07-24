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
using Miki.Framework.Extension;
using Miki.API;
using System.Collections.Concurrent;
using Miki.Accounts.Achievements;
using Miki.Framework.Languages;
using Miki.Accounts.Achievements.Objects;
using Miki.Framework.Events.Commands;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Cache.StackExchange;
using StackExchange.Redis;
using Miki.API.Gambling;

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
					long size = (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListLength(lotteryKey);

					if (size == 0)
						return;

					string value = (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListGetByIndex(lotteryKey, MikiRandom.Next(size));

					ulong winnerId = ulong.Parse(value);
					int wonAmount = (int)Math.Round(size * 100 * 0.75);

					IDiscordUser user = null; //Bot.Instance.Client.GetUser(winnerId);

					using (var context = new MikiContext())
					{
						long id = winnerId.ToDbLong();
						User profileUser = context.Users.Find(id);

						if (user != null)
						{
							IDiscordChannel channel = user.GetDMChannel().Result;

							EmbedBuilder embed = new EmbedBuilder()
							{
								Author = new EmbedAuthor()
								{
									Name = "Winner winner chicken dinner",
									IconUrl = user.GetAvatarUrl()
								},
								Description = $"Wow! You won the lottery and gained {wonAmount} mekos!"
							};

							profileUser.AddCurrencyAsync(wonAmount, channel);

							embed.ToEmbed().QueueToChannel(channel);

							context.SaveChanges();

							Global.RedisClient.RemoveAsync(lotteryKey);
							Global.RedisClient.UpsertAsync("lottery:winner", profileUser.Name ?? "unknown");
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
			int? bet = await ValidateBetAsync(e, e.Arguments.FirstOrDefault(), 10000);
			if(bet.HasValue)
			{
				await StartRPS(e, bet.Value);
			}
		}

		public async Task StartRPS(EventContext e, int bet)
		{
			float rewardMultiplier = 1f;

			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbed("You need to choose a weapon!")
					.ToEmbed().QueueToChannel(e.Channel);
			}
			else
			{
				User user;
				RPSManager rps = RPSManager.Instance;
				EmbedBuilder resultMessage = Utils.Embed
					.SetTitle("Rock, Paper, Scissors!");

				if (rps.TryParse(e.Arguments.Get(1).Argument, out RPSWeapon playerWeapon))
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
					resultMessage.SetDescription("Invalid weapon!").ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}
				resultMessage.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "blackjack", Aliases = new[] { "bj" })]
		public async Task BlackjackAsync(EventContext e)
		{
			ArgObject args = e.Arguments.FirstOrDefault();
			string subCommand = args?.Argument.ToLower() ?? "";
			args = args?.Next();

			switch (subCommand)
			{
				case "new":
				{
					await OnBlackjackNew(e, args);
				} break;
				case "hit":
				case "draw":
				{
					await OnBlackjackHit(e);
				} break;
				case "stay":
				case "stand":
				{
					await OnBlackjackHold(e);
				} break;
				default:
				{
					new EmbedBuilder()
						.SetTitle("🎲 Blackjack")
						.SetColor(234, 89, 110)
						.SetDescription("Play a game of blackjack against miki!\n\n" +
							"`>blackjack new <bet> [ok]` to start a new game\n" +
							"`>blackjack hit` to draw a card\n" +
							"`>blackjack stay` to stand")
						.ToEmbed()
						.QueueToChannel(e.Channel);
				} break;
			}

		}

		public async Task OnBlackjackNew(EventContext e, ArgObject args)
		{
			int? bet = await ValidateBetAsync(e, args);

			if (bet.HasValue)
			{
				using(var context = new MikiContext())
				{
					var user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
					
					if(user == null)
						return;
					
					user.Currency -= bet.Value;
					
					await context.SaveChangesAsync();	
				}
				
				if(await Global.RedisClient.ExistsAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}"))
				{
					e.ErrorEmbed("You still have a blackjack game running here, please either stop it by using `>blackjack stay` or finish playing it. This game will expire in 24 hours.")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				BlackjackManager manager = new BlackjackManager(bet.Value);

				CardHand dealer = manager.AddPlayer(0);
				CardHand player = manager.AddPlayer(e.Author.Id);

				manager.DealAll();
				manager.DealAll();

				dealer.Hand[1].isPublic = false;

				IDiscordMessage message = await manager.CreateEmbed(e)
					.ToEmbed()
					.SendToChannel(e.Channel);

				manager.MessageId = message.Id;

				await Global.RedisClient.UpsertAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}", manager.ToContext(), TimeSpan.FromHours(24));
			}
		}

		private async Task OnBlackjackHit(EventContext e)
		{
			BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(Global.RedisClient, e.Channel.Id, e.Author.Id);

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

				await Global.Client.Client._apiClient.EditMessageAsync(e.Channel.Id, bm.MessageId, new EditMessageArgs
				{
					embed = bm.CreateEmbed(e).ToEmbed()
				});

				await Global.RedisClient.UpsertAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}", bm.ToContext(), TimeSpan.FromHours(24));
			}
		}

		private async Task OnBlackjackHold(EventContext e, bool charlie = false)
		{
			BlackjackManager bm = await BlackjackManager.FromCacheClientAsync(Global.RedisClient, e.Channel.Id, e.Author.Id);

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
			await e.EventSystem.GetCommandHandler<SessionBasedCommandHandler>()
				.RemoveSessionAsync(e.Author.Id, e.Channel.Id);

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

			await Global.Client.Client._apiClient.EditMessageAsync(e.Channel.Id, bm.MessageId, 
				new EditMessageArgs
				{
					embed = bm.CreateEmbed(e)
				   .SetAuthor(
						e.GetResource("miki_blackjack_draw_title") + " | " + e.Author.Username, 
						e.Author.GetAvatarUrl(), 
						"https://patreon.com/mikibot"
					)
				   .SetDescription(
						e.GetResource("blackjack_draw_description") + "\n" +
						e.GetResource("miki_blackjack_current_balance", user.Currency)
					).ToEmbed()
				}
			);

			await Global.RedisClient.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");
		}

		private async Task OnBlackjackDead(EventContext e, BlackjackManager bm)
		{
			User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
			}
			await Global.Client.Client._apiClient.EditMessageAsync(e.Channel.Id, bm.MessageId,
				new EditMessageArgs
				{
					embed = bm.CreateEmbed(e)
							.SetAuthor(
								e.GetResource("miki_blackjack_lose_title") + " | " + e.Author.Username,
								(await e.Guild.GetSelfAsync()).GetAvatarUrl(), "https://patreon.com/mikibot"
							)
							.SetDescription(
								e.GetResource("miki_blackjack_lose_description") + "\n" + e.GetResource("miki_blackjack_new_balance",
								user.Currency)
							).ToEmbed()
				});

			await Global.RedisClient.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");
		}

		private async Task OnBlackjackWin(EventContext e, BlackjackManager bm)
		{
			User user;
			using (var context = new MikiContext())
			{
				user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (user != null)
				{
					await user.AddCurrencyAsync(bm.Bet * 2, e.Channel);
					await context.SaveChangesAsync();
				}
			}

			await Global.Client.Client._apiClient.EditMessageAsync(e.Channel.Id, bm.MessageId, new EditMessageArgs
			{
				embed = bm.CreateEmbed(e)
					.SetAuthor(e.GetResource("miki_blackjack_win_title") + " | " + e.Author.Username, e.Author.GetAvatarUrl(), "https://patreon.com/mikibot")
					.SetDescription(e.GetResource("miki_blackjack_win_description", bm.Bet * 2) + "\n" + e.GetResource("miki_blackjack_new_balance", user.Currency))
					.ToEmbed()
			});

			await Global.RedisClient.RemoveAsync($"miki:blackjack:{e.Channel.Id}:{e.Author.Id}");
		}

		[Command(Name = "flip")]
		public async Task FlipAsync(EventContext e)
		{
			int? bet = await ValidateBetAsync(e, e.Arguments.FirstOrDefault(), 10000);
			if (bet.HasValue)
			{
				await StartFlip(e, bet.Value);
			}
		}

		private async Task StartFlip(EventContext e, int bet)
		{
			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbed("Please pick either `heads` or `tails`!")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			string sideParam = e.Arguments.Get(1).Argument.ToLower();

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
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			string headsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-heads.png";
			string tailsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-default-tails.png";

			if (e.Arguments.Contains("-bonus"))
			{
				headsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-secret-heads.png";
				tailsUrl = "https://miki-cdn.nyc3.digitaloceanspaces.com/commands/miki-secret-tails.png";
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

			DiscordEmbed embed = Utils.Embed
				.SetAuthor(e.GetResource("flip_header") + " | " + e.Author.Username, e.Author.GetAvatarUrl(),
					"https://patreon.com/mikibot")
				.SetDescription(output)
				.SetThumbnail(imageUrl)
				.ToEmbed();

			embed.QueueToChannel(e.Channel);
		}

		[Command(Name = "slots", Aliases = new[] { "s" })]
		public async Task SlotsAsync(EventContext e)
		{
			ArgObject argObject = e.Arguments.FirstOrDefault();
			int? i = await ValidateBetAsync(e, argObject, 99999);
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

				embed.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "lottery")]
		public async Task LotteryAsync(EventContext e)
		{
			ArgObject arg = e.Arguments.FirstOrDefault();

			if (arg == null)
			{
				long totalTickets = await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListLengthAsync(lotteryKey);
				long yourTickets = 0;

				string latestWinner = (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).StringGet("lottery:winner");

				if (await lotteryDict.ContainsAsync(e.Author.Id))
				{
					yourTickets = long.Parse(await lotteryDict.GetAsync(e.Author.Id));
				}

				string timeLeft = taskScheduler?.GetInstance(0, lotteryId).TimeLeft.ToTimeString(e.Channel.Id, true) ?? "1h?m?s - will be fixed soon!";

				new EmbedBuilder()
				{
					Title = "🍀 Lottery",
					Description = "Make the biggest gamble, and get paid off massively if legit.",
					Color = new Color(119, 178, 85)
				}.AddInlineField("Tickets Owned", yourTickets)
				.AddInlineField("Drawing In", timeLeft)
				.AddInlineField("Total Tickets", totalTickets)
				.AddInlineField("Ticket price", $"{100} mekos")
				.AddInlineField("Latest Winner", latestWinner ?? "no name")
				.AddInlineField("How to buy?", ">lottery buy [amount]")
				.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			switch (arg.Argument.ToLower())
			{
				case "buy":
				{
					arg = arg.Next();
					int amount = arg?.AsInt() ?? 1;

					if (amount < 1)
						amount = 1;

					using (var context = new MikiContext())
					{
						User u = await User.GetAsync(context, e.Author);

						if (amount * 100 > u.Currency)
						{
							e.ErrorEmbedResource("miki_mekos_insufficient")
								.ToEmbed().QueueToChannel(e.Channel);
							return;
						}

						await u.AddCurrencyAsync(-amount * 100, e.Channel);

						RedisValue[] tickets = new RedisValue[amount];

						for (int i = 0; i < amount; i++)
						{
							tickets[i] = e.Author.Id.ToString();
						}

						await (Global.RedisClient as StackExchangeCacheClient).Client.GetDatabase(0).ListRightPushAsync(lotteryKey, tickets);

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

		public async Task<int?> ValidateBetAsync(EventContext e, ArgObject arg, int maxBet = 1000000)
		{
			if (arg != null)
			{
				const int noAskLimit = 10000;

				using (MikiContext context = new MikiContext())
				{
					User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

					if (user == null)
					{
						// TODO: add user null error
						return null;
					}

					string checkArg = arg?.Argument;

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
							.ToEmbed().QueueToChannel(e.Channel);
						return null;
					}

					if (bet < 1)
					{
						e.ErrorEmbed(e.GetResource("miki_error_gambling_zero_or_less"))
							.ToEmbed().QueueToChannel(e.Channel);
						return null;
					}
					else if (bet > user.Currency)
					{
						e.ErrorEmbed(e.GetResource("miki_mekos_insufficient"))
							.ToEmbed().QueueToChannel(e.Channel);
						return null;
					}
					else if (bet > maxBet)
					{
						e.ErrorEmbed($"you cannot bet more than {maxBet} mekos!")
							.ToEmbed().QueueToChannel(e.Channel);
						return null;
					}
					else if (bet > noAskLimit)
					{
						arg = arg?.Next();
						if (arg?.Argument.ToLower() == "ok")
						{
							return bet;
						}
						else
						{
							EmbedBuilder embed = Utils.Embed;
							embed.Description =
								$"Are you sure you want to bet **{bet}**? You currently have `{user.Currency}` mekos.\n\nAppend your command with `>my_command ... <bet> ok` to confirm.";
							embed.Color = new Color(0.4f, 0.6f, 1f);
							embed.ToEmbed().QueueToChannel(e.Channel);
							return null;
						}
					}
					else
					{
						return bet;
					}
				}
			}
			else
			{
				e.ErrorEmbed(e.GetResource("miki_error_gambling_no_arg"))
					.ToEmbed().QueueToChannel(e.Channel);
				return null;
			}
		}
	}
}
