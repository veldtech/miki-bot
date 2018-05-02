#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Discord;
using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.API.Leaderboards;
using Miki.Common.Builders;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Extension;
using Miki.Framework.Languages;
using Miki.Models;
using Miki.Models.Objects.Backgrounds;
using Miki.Models.Objects.User;
using Miki.Modules.Accounts.Services;
using Miki.Rest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Miki.Modules.AccountsModule
{
	[Module("Accounts")]
	public class AccountsModule
	{
		BackgroundStore store = new BackgroundStore();

		RestClient client = new RestClient(Global.Config.ImageApiUrl)
			.AddHeader("Authorization", Global.Config.MikiApiKey);

		EmojiBarSet onBarSet = new EmojiBarSet(
			"<:mbarlefton:391971424442646534>",
			"<:mbarmidon:391971424920797185>",
			"<:mbarrighton:391971424488783875>");

		EmojiBarSet offBarSet = new EmojiBarSet(
			"<:mbarleftoff:391971424824459265>",
			"<:mbarmidoff:391971424824197123>",
			"<:mbarrightoff:391971424862208000>");

		// TODO: install services automatically.
		public AccountsModule(Module module)
		{
			new AchievementsService()
				.Install(module);

			new ExperienceTrackerService()
				.Install(module);
		}

		[Command(Name = "achievements")]
		public async Task AchievementsAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
					IUser user = await arg.TakeUntilEnd()
						.GetUserAsync(e.Guild);

					if (user != null)
					{
						id = (long)user.Id;
					}
				}

				IUser discordUser = await e.Guild.GetUserAsync(id.FromDbLong());
				User u = await User.GetAsync(context, discordUser);

				List<Achievement> achievements = await context.Achievements
					.Where(x => x.Id == id)
					.ToListAsync();

				EmbedBuilder embed = Utils.Embed
					.SetAuthor($"{u.Name} | " + "Achievements", discordUser.GetAvatarUrl(), "https://miki.ai/profiles/ID/achievements");

				embed.WithColor(255, 255, 255);

				StringBuilder leftBuilder = new StringBuilder();

				int totalScore = 0;

				foreach (var a in achievements)
				{
					BaseAchievement metadata = AchievementManager.Instance.GetContainerById(a.Name).Achievements[a.Rank];
					leftBuilder.AppendLine(metadata.Icon + " | `" + metadata.Name.PadRight(15) + $"{metadata.Points.ToString().PadLeft(3)} pts` | 📅 {a.UnlockedAt.ToShortDateString()}");
					totalScore += metadata.Points;
				}

				if (string.IsNullOrEmpty(leftBuilder.ToString()))
				{
					embed.AddInlineField("Total Pts: " + totalScore, "None, yet.");
				}
				else
				{
					embed.AddInlineField("Total Pts: " + totalScore, leftBuilder.ToString());
				}

				embed.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "exp")]
		public async Task ExpAsync(EventContext e)
		{
			if (!await Global.RedisClient.ExistsAsync($"user:{e.Author.Id}:avatar:synced"))
				await Utils.SyncAvatarAsync(e.Author);

			Stream s = await client.GetStreamAsync("api/user?id=" + e.Author.Id);
			if (s == null)
			{
				e.ErrorEmbed("Image generation API did not respond. This is an issue, please report it.")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			await e.Channel.SendFileAsync(s, "exp.png");
		}

		[Command(Name = "leaderboards", Aliases = new[] { "lb", "leaderboard", "top" })]
		public async Task LeaderboardsAsync(EventContext e)
		{
			LeaderboardsOptions options = new LeaderboardsOptions();

			ArgObject argument = e.Arguments.FirstOrDefault();

			switch (argument?.Argument.ToLower() ?? "")
			{
				case "commands":
				case "cmds":
				{
					options.type = LeaderboardsType.COMMANDS;
					argument = argument?.Next();
				}
				break;

				case "currency":
				case "mekos":
				case "money":
				case "bal":
				{
					options.type = LeaderboardsType.CURRENCY;
					argument = argument?.Next();
				}
				break;

				case "rep":
				case "reputation":
				{
					options.type = LeaderboardsType.REPUTATION;
					argument = argument?.Next();
				}
				break;

				case "pasta":
				case "pastas":
				{
					options.type = LeaderboardsType.PASTA;
					argument = argument?.Next();
				}
				break;

				case "experience":
				case "exp":
				{
					options.type = LeaderboardsType.EXPERIENCE;
					argument = argument?.Next();
				}
				break;

				case "guild":
				case "guilds":
				{
					options.type = LeaderboardsType.GUILDS;
					argument = argument?.Next();
				}
				break;


				default:
				{
					options.type = LeaderboardsType.EXPERIENCE;
				}
				break;
			}

			if (argument?.Argument.ToLower() == "local")
			{
				if (options.type != LeaderboardsType.PASTA)
				{
					options.guildId = e.Guild.Id;
				}
				argument = argument.Next();
			}

			// Null-conditional operators do not apply on async methods.
			if (argument != null)
			{
				IUser user = await argument.GetUserAsync(e.Guild);
				if (user != null)
				{
					options.mentionedUserId = user.Id;
					argument = argument.Next();
				}
			}

			if ((argument?.AsInt() ?? 0) != 0)
			{
				options.pageNumber = argument.AsInt().Value - 1;
				argument = argument?.Next();
			}

			await ShowLeaderboardsAsync(e.message, options);
		}

		// TODO: rework, or atleast clean up.
		[Command(Name = "profile")]
		public async Task ProfileAsync(EventContext e)
		{
			Stopwatch sw = new Stopwatch();

			sw.Start();

			using (var context = new MikiContext())
			{
				long id = 0;
				ulong uid = 0;

				var arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
					uid = (await arg.GetUserAsync(e.Guild)).Id;
					id = uid.ToDbLong();
				}
				else
				{
					uid = e.message.Author.Id;
					id = uid.ToDbLong();
				}

				IUser discordUser = await e.Guild.GetUserAsync(uid);
				User account = await User.GetAsync(context, discordUser);

				string icon = "";

				if(await account.IsDonatorAsync(context))
				{
					icon = "https://cdn.discordapp.com/emojis/421969679561785354.png";
				}

				if (account != null)
				{
					EmbedBuilder embed = Utils.Embed
						.WithDescription(account.Title)
						.SetAuthor(e.GetResource("miki_global_profile_user_header", account.Name), icon, "https://patreon.com/mikibot")
						.WithThumbnailUrl(discordUser.GetAvatarUrl());

					long serverid = e.Guild.Id.ToDbLong();

					LocalExperience localExp = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), discordUser);
					if(localExp == null)
					{
						localExp = await LocalExperience.CreateAsync(context, serverid, discordUser);
					}

					int rank = await localExp.GetRank(context);
					int localLevel = User.CalculateLevel(localExp.Experience);
					int maxLocalExp = User.CalculateLevelExperience(localLevel);
					int minLocalExp = User.CalculateLevelExperience(localLevel - 1);

					EmojiBar expBar = new EmojiBar(maxLocalExp - minLocalExp, onBarSet, offBarSet, 6);

					string infoValue = new MessageBuilder()
						.AppendText(e.GetResource("miki_module_accounts_information_level", localLevel, localExp.Experience, maxLocalExp))
						.AppendText(await expBar.Print(localExp.Experience - minLocalExp, e.Channel))
						.AppendText(e.GetResource("miki_module_accounts_information_rank", rank))
						.AppendText("Reputation: " + account.Reputation, MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(e.GetResource("miki_generic_information"), infoValue);

					int globalLevel = User.CalculateLevel(account.Total_Experience);
					int maxGlobalExp = User.CalculateLevelExperience(globalLevel);
					int minGlobalExp = User.CalculateLevelExperience(globalLevel -1);

					int globalRank = await account.GetGlobalRankAsync();

					EmojiBar globalExpBar = new EmojiBar(maxGlobalExp - minGlobalExp, onBarSet, offBarSet, 6);

					string globalInfoValue = new MessageBuilder()
						.AppendText(e.GetResource("miki_module_accounts_information_level", globalLevel, account.Total_Experience, maxGlobalExp))
						.AppendText(await globalExpBar.Print(account.Total_Experience - minGlobalExp, e.Channel))
						.AppendText(e.GetResource("miki_module_accounts_information_rank", globalRank), MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(e.GetResource("miki_generic_global_information"), globalInfoValue);
					embed.AddInlineField(e.GetResource("miki_generic_mekos"), account.Currency + "<:mekos:421972155484471296>");

					List<UserMarriedTo> Marriages = await Marriage.GetMarriagesAsync(context, id);

					Marriages.RemoveAll(x => x.Marriage.IsProposing);

					List<string> users = new List<string>();

					int maxCount = Marriages?.Count ?? 0;

					for (int i = 0; i < maxCount; i++)
					{
						users.Add(await User.GetNameAsync(context, Marriages[i].GetOther(id)));
					}

					if (Marriages?.Count > 0)
					{
						List<string> MarriageStrings = new List<string>();

						for (int i = 0; i < maxCount; i++)
						{
							if (Marriages[i].GetOther(id) != 0)
							{
								MarriageStrings.Add($"💕 {users[i]} (_{Marriages[i].Marriage.TimeOfMarriage.ToShortDateString()}_)");
							}
						}

						string marriageText = string.Join("\n", MarriageStrings);
						if(string.IsNullOrEmpty(marriageText))
						{
							marriageText = e.GetResource("miki_placeholder_null");
						}

						embed.AddInlineField(
							e.GetResource("miki_module_accounts_profile_marriedto"),
							marriageText);
					}

					Random r = new Random((int)id - 3);

					embed.Color = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

					CommandUsage favouriteCommand = await context.CommandUsages
						.OrderByDescending(x => x.Amount)
						.FirstOrDefaultAsync(x => x.UserId == id);

					string favCommand = $"{favouriteCommand?.Name ?? e.GetResource("miki_placeholder_null")} ({ favouriteCommand?.Amount ?? 0 })";

					embed.AddInlineField(e.GetResource("miki_module_accounts_profile_favourite_command"),
						favCommand);

					List<Achievement> allAchievements = await context.Achievements.Where(x => x.Id == id).ToListAsync();

					string achievements = e.GetResource("miki_placeholder_null");

					if (allAchievements != null)
					{
						if (allAchievements.Count > 0)
						{
							achievements = AchievementManager.Instance.PrintAchievements(allAchievements);
						}
					}

					embed.AddInlineField(
						e.GetResource("miki_generic_achievements"),
						achievements);

					embed.WithFooter(
						e.GetResource("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(),
							sw.ElapsedMilliseconds), "");

					sw.Stop();

					embed.Build().QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed(e.GetResource("error_account_null"))
						.Build().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "setbackground")]
		public async Task SetProfileBackgroundAsync(EventContext e)
		{
			int? backgroundId = e.Arguments.First().AsInt();

			if (backgroundId == null)
				throw new ArgumentNullException("background");

			long userId = e.Author.Id.ToDbLong();
			using (var context = new MikiContext())
			{
				BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, backgroundId ?? 0, context);

				if (bo == null)
				{
					throw new BackgroundNotOwnedException();
				}

				ProfileVisuals v = await ProfileVisuals.GetAsync(userId, context);
				v.BackgroundId = bo.BackgroundId;

				await context.SaveChangesAsync();
			}

			Utils.SuccessEmbed(e.Channel.Id, "Successfully set background.")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "buybackground")]
		public async Task BuyProfileBackgroundAsync(EventContext e)
		{
			int? backgroundId = e.Arguments.First().AsInt();

			if (backgroundId.HasValue)
			{
				Background background = Global.Backgrounds.Backgrounds[backgroundId.Value];


				var embed = new EmbedBuilder();
				embed.WithTitle("Buy Background");

				if (background.Price > 0)
				{
					embed.WithDescription($"This background for your profile will cost {background.Price} mekos, Type yes to buy.");
				}
				else
				{
					embed.WithDescription($"This background is not for sale.");
				}
				embed.WithImageUrl(background.ImageUrl)
					.Build().QueueToChannel(e.Channel);

				if (background.Price > 0)
				{
					IMessage msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

					if (msg.Content.ToLower()[0] == 'y')
					{
						using (var context = new MikiContext())
						{
							User user = await User.GetAsync(context, e.Author);
							long userId = e.Author.Id.ToDbLong();

							BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, background.Id);

							if (bo == null)
							{
								await user.AddCurrencyAsync(-background.Price, e.Channel);
								await context.BackgroundsOwned.AddAsync(new BackgroundsOwned()
								{
									UserId = e.Author.Id.ToDbLong(),
									BackgroundId = background.Id,
								});
								await context.SaveChangesAsync();

								Utils.SuccessEmbed(e.Channel.Id, "Background purchased!")
									.QueueToChannel(e.Channel);
							}
							else
							{
								throw new BackgroundOwnedException();
							}
						}
					}
				}
			}
		}

		[Command(Name = "setbackcolor")]
		public async Task SetProfileBackColorAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, e.Author);

				new EmbedBuilder()
					.WithTitle("Hold on!")
					.WithDescription("Changing your background color costs 250 mekos. type a hex to purchase")
					.Build().QueueToChannel(e.Channel);

				IMessage msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

				var x = Regex.Matches(msg.Content.ToUpper(), "(#)?([A-F0-9]{6})");

				if (x.Count > 0)
				{
					ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.Author.Id, context);
					var hex = x.First().Groups.Last().Value;

					visuals.BackgroundColor = hex;
					await user.AddCurrencyAsync(-250, e.Channel);
					await context.SaveChangesAsync();

					Utils.SuccessEmbed(e.Channel.Id,
						$"Your background color has been successfully changed to `{hex}`")
						.QueueToChannel(e.Channel);
				}
				else
				{
					throw new ArgumentException("Argument was not a hex color");
				}
			}
		}

		[Command(Name = "setfrontcolor")]
		public async Task SetProfileForeColorAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, e.Author);

				new EmbedBuilder()
					.WithTitle("Hold on!")
					.WithDescription("Changing your foreground(text) color costs 250 mekos. type a hex(e.g. #00FF00) to purchase")
					.Build().QueueToChannel(e.Channel);

				IMessage msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

				var x = Regex.Matches(msg.Content.ToUpper(), "(#)?([A-F0-9]{6})");

				if (x.Count > 0)
				{
					ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.Author.Id, context);
					var hex = x.First().Groups.Last().Value;

					visuals.ForegroundColor = hex;
					await user.AddCurrencyAsync(-250, e.Channel);
					await context.SaveChangesAsync();

					Utils.SuccessEmbed(e.Channel.Id,
						$"Your foreground color has been successfully changed to `{hex}`")
						.QueueToChannel(e.Channel);
				}
				else
				{
					throw new ArgumentException("Argument was not a hex color");
				}
			}
		}

		[Command(Name = "backgroundsowned")]
		public async Task BackgroundsOwnedAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				List<BackgroundsOwned> backgroundsOwned = await context.BackgroundsOwned.Where(x => x.UserId == e.Author.Id.ToDbLong())
					.ToListAsync();

				new EmbedBuilder()
					.WithTitle($"{e.Author.Username}'s backgrounds")
					.WithDescription(string.Join(",", backgroundsOwned.Select(x => $"`{x.BackgroundId}`" )))
					.Build()
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "rep")]
		public async Task GiveReputationAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User giver = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				var repObject = Global.RedisClient.Get<ReputationObject>($"user:{giver.Id}:rep");

				if (repObject == null)
				{
					repObject = new ReputationObject()
					{
						LastReputationGiven = DateTime.Now,
						ReputationPointsLeft = 3
					};
					await Global.RedisClient.AddAsync($"user:{giver.Id}:rep", repObject, new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date));
				}

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg == null)
				{
					TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

					new EmbedBuilder()
					{
						Title = (e.GetResource("miki_module_accounts_rep_header")),
						Description = (e.GetResource("miki_module_accounts_rep_description"))
					}.AddInlineField(e.GetResource("miki_module_accounts_rep_total_received"), giver.Reputation.ToString())
						.AddInlineField(e.GetResource("miki_module_accounts_rep_reset"), pointReset.ToTimeString(e.Channel.Id))
						.AddInlineField(e.GetResource("miki_module_accounts_rep_remaining"), repObject.ReputationPointsLeft)
						.Build().QueueToChannel(e.Channel);
					return;
				}
				else
				{
					Dictionary<IUser, int> usersMentioned = new Dictionary<IUser, int>();

					EmbedBuilder embed = new EmbedBuilder();

					int totalAmountGiven = 0;
					bool mentionedSelf = false;

					while (true || totalAmountGiven <= repObject.ReputationPointsLeft)
					{
						if (arg == null)
							break;

						IUser u = await arg.GetUserAsync(e.Guild);
						int amount = 1;

						if (u == null)
							break;

						arg = arg?.Next();

						if ((arg?.AsInt() ?? -1) != -1)
						{
							amount = arg.AsInt().Value;
							arg = arg.Next();
						}
						else if (Utils.IsAll(arg))
						{
							amount = repObject.ReputationPointsLeft;
							arg = arg.Next();
						}

						if (u.Id == e.Author.Id)
						{
							mentionedSelf = true;
							continue;
						}

						totalAmountGiven += amount;

						if (usersMentioned.Keys.Where(x => x.Id == u.Id).Count() > 0)
						{
							usersMentioned[usersMentioned.Keys.Where(x => x.Id == u.Id).First()] += amount;
						}
						else
						{
							usersMentioned.Add(u, amount);
						}
					}

					if (mentionedSelf)
					{
						embed.Footer = new EmbedFooterBuilder()
						{
							Text = e.GetResource("warning_mention_self"),
						};
					}

					if (usersMentioned.Count == 0)
					{
						return;
					}
					else
					{
						if (totalAmountGiven <= 0)
						{
							e.ErrorEmbedResource("miki_module_accounts_rep_error_zero")
								.Build().QueueToChannel(e.Channel);
							return;
						}

						if (usersMentioned.Sum(x => x.Value) > repObject.ReputationPointsLeft)
						{
							e.ErrorEmbedResource("error_rep_limit", usersMentioned.Count, usersMentioned.Sum(x => x.Value), repObject.ReputationPointsLeft)
								.Build().QueueToChannel(e.Channel);
							return;
						}
					}

					embed.Title = (e.GetResource("miki_module_accounts_rep_header"));
					embed.Description = (e.GetResource("rep_success"));

					foreach (var user in usersMentioned)
					{
						User receiver = await User.GetAsync(context, user.Key);

						receiver.Reputation += user.Value;

						embed.AddInlineField(receiver.Name, string.Format("{0} => {1} (+{2})", receiver.Reputation - user.Value, receiver.Reputation, user.Value));
					}

					repObject.ReputationPointsLeft -= (short)(usersMentioned.Sum(x => x.Value));

					await Global.RedisClient.AddAsync($"user:{giver.Id}:rep", repObject, new DateTimeOffset(DateTime.UtcNow.AddDays(1).Date));

					embed.AddInlineField(e.GetResource("miki_module_accounts_rep_points_left"), repObject.ReputationPointsLeft)
						.Build().QueueToChannel(e.Channel);

					await context.SaveChangesAsync();
				}
			}
		}

		[Command(Name = "syncname")]
		public async Task SyncNameAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (user == null)
				{
					return;
				}

				user.Name = e.Author.Username;
				await context.SaveChangesAsync();
			}

			EmbedBuilder embed = Utils.Embed;
			embed.Title = "👌 OKAY";
			embed.Description = e.GetResource("sync_success", "name");	
			embed.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "mekos", Aliases = new string[] { "bal", "meko" })]
		public async Task ShowMekosAsync(EventContext e)
		{
			ulong targetId = e.message.MentionedUserIds.Count > 0 ? e.message.MentionedUserIds.First() : 0;

			if (e.message.MentionedUserIds.Count > 0)
			{
				if (targetId == 0)
				{
					e.ErrorEmbedResource("miki_module_accounts_mekos_no_user")
						.Build().QueueToChannel(e.Channel);
					return;
				}
				IUser userCheck = await e.Guild.GetUserAsync(targetId);
				if (userCheck.IsBot)
				{
					e.ErrorEmbedResource("miki_module_accounts_mekos_bot")
						.Build().QueueToChannel(e.Channel);
					return;
				}
			}

			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, await e.Guild.GetUserAsync(targetId != 0 ? targetId : e.Author.Id));

				new EmbedBuilder()
				{
					Title = "🔸 Mekos",
					Description = e.GetResource("miki_user_mekos", user.Name, user.Currency),
					Color = new Color(1f, 0.5f, 0.7f)
				}.Build().QueueToChannel(e.Channel);

				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "give")]
		public async Task GiveMekosAsync(EventContext e)
		{
			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbedResource("give_error_no_arg")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			ArgObject arg = e.Arguments.FirstOrDefault();

			IUser user = null;

			if (arg != null)
			{
				user = await arg.GetUserAsync(e.Guild);
			}

			if (user == null)
			{
				e.ErrorEmbedResource("give_error_no_mention")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			arg = arg.Next();

			int? amount = arg?.AsInt() ?? null;

			if (amount == null)
			{
				e.ErrorEmbedResource("give_error_amount_unparsable")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			if (amount <= 0)
			{
				e.ErrorEmbedResource("give_error_min_mekos")
					.Build().QueueToChannel(e.Channel);
				return;
			}

			using (MikiContext context = new MikiContext())
			{
				User sender = await User.GetAsync(context, e.Author);
				User receiver = await User.GetAsync(context, user);

				if (amount.Value <= sender.Currency)
				{
					await sender.AddCurrencyAsync(-amount.Value, e.Channel, sender);
					await receiver.AddCurrencyAsync(amount.Value, e.Channel, sender);

					new EmbedBuilder()
					{
						Title = "🔸 transaction",
						Description = e.GetResource("give_description", sender.Name, receiver.Name, amount.Value),
						Color = new Color(255, 140, 0),
					}.Build().QueueToChannel(e.Channel);

					await context.SaveChangesAsync();
				}
				else
				{
					e.ErrorEmbedResource("user_error_insufficient_mekos")
						.Build().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "daily")]
		public async Task GetDailyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User u = await User.GetAsync(context, e.Author);

				if (u == null)
				{
					e.ErrorEmbed(e.GetResource("user_error_no_account"))
						.Build().QueueToChannel(e.Channel);
					return;
				}

				int dailyAmount = 100;
				int dailyStreakAmount = 20;

				if (await u.IsDonatorAsync(context))
				{
					dailyAmount *= 2;
					dailyStreakAmount *= 2;
				}

				if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
				{
					e.ErrorEmbed($"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.Channel.Id)}` before using it again.")
					.AddInlineField("Need more mekos?", "Vote for us every day on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote) for a bonus daily!").Build().QueueToChannel(e.Channel);
					return;
				}

				int streak = 0;
				string redisKey = $"user:{e.Author.Id}:daily";

				if (await Global.RedisClient.ExistsAsync(redisKey))
				{
					streak = await Global.RedisClient.GetAsync<int>(redisKey);
					streak++;
				}

				int amount = dailyAmount + (dailyStreakAmount * Math.Min(100, streak));

				await u.AddCurrencyAsync(amount, e.Channel);
				u.LastDailyTime = DateTime.Now;

				var embed = Utils.Embed.WithTitle("💰 Daily")
					.WithDescription($"Received **{amount}** Mekos! You now have `{u.Currency}` Mekos")
					.WithColor(253, 216, 136);

				if (streak > 0)
				{
					embed.AddInlineField("Streak!", $"You're on a {streak} day daily streak!");
				}

				embed.AddInlineField("Need more mekos?", "Vote for us every day on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote) for a bonus daily!");

				embed.Build().QueueToChannel(e.Channel);

				await Global.RedisClient.AddAsync(redisKey, streak, new TimeSpan(48, 0, 0));
				await context.SaveChangesAsync();
			}
		}

		//[Command(Name = "mybadges")]
		//public async Task MyBadgesAsync(EventContext e)
		//{
		//	int page = 0;
		//	using (var context = new MikiContext())
		//	{
		//		User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

		//		string output = string.Join<long>(" ", u.BadgesOwned.Select(x => x.Id).ToList());

		//		await e.Channel.SendMessage(output.DefaultIfEmpty("none, yet!"));
		//	}
		//}

		public async Task ShowLeaderboardsAsync(IMessage mContext, LeaderboardsOptions leaderboardOptions)
		{
			using (var context = new MikiContext())
			{
				int p = Math.Max(leaderboardOptions.pageNumber - 1, 0);

				LeaderboardsObject obj = await Global.MikiApi.GetPagedLeaderboardsAsync(leaderboardOptions);

				Utils.RenderLeaderboards(Utils.Embed, obj.items, obj.currentPage * 10)
					.WithFooter(Locale.GetString(mContext.Channel.Id, "page_index", obj.currentPage + 1, Math.Ceiling((double)obj.totalItems / 10)), "")
					.WithAuthor(
						"Leaderboards: " + leaderboardOptions.type + " (click me!)",
						null,
						Global.MikiApi.BuildLeaderboardsUrl(leaderboardOptions)
					)
					.Build().QueueToChannel(mContext.Channel);
			}
		}
	}
}