#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Microsoft.EntityFrameworkCore;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.API;
using Miki.API.Leaderboards;
using Miki.Bot.Models.Repositories;
using Miki.Common.Builders;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Helpers;
using Miki.Models;
using Miki.Models.Objects.Backgrounds;
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
		[Service("experience")]
		public ExperienceTrackerService ExperienceService { get; set; }

		[Service("achievements")]
		public AchievementsService AchievementsService { get; set; }

		private readonly BackgroundStore store = new BackgroundStore();

		private RestClient client = new RestClient(Global.Config.ImageApiUrl)
			.AddHeader("Authorization", Global.Config.MikiApiKey);

		private readonly EmojiBarSet onBarSet = new EmojiBarSet(
			"<:mbarlefton:391971424442646534>",
			"<:mbarmidon:391971424920797185>",
			"<:mbarrighton:391971424488783875>");

		private readonly EmojiBarSet offBarSet = new EmojiBarSet(
			"<:mbarleftoff:391971424824459265>",
			"<:mbarmidoff:391971424824197123>",
			"<:mbarrightoff:391971424862208000>");

		[Command(Name = "achievements")]
		public async Task AchievementsAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long id = (long)e.Author.Id;

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg != null)
				{
					IDiscordUser user = await arg.TakeUntilEnd()
						.GetUserAsync(e.Guild);

					if (user != null)
					{
						id = (long)user.Id;
					}
				}

				IDiscordUser discordUser = await e.Guild.GetMemberAsync(id.FromDbLong());
				User u = await User.GetAsync(context, discordUser.Id, discordUser.Username);

				List<Achievement> achievements = await context.Achievements
					.Where(x => x.Id == id)
					.ToListAsync();

				EmbedBuilder embed = new EmbedBuilder()
					.SetAuthor($"{u.Name} | " + "Achievements", discordUser.GetAvatarUrl(), "https://miki.ai/profiles/ID/achievements");

				embed.SetColor(255, 255, 255);

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
					embed.AddInlineField("Total Pts: " + totalScore.ToFormattedString(), "None, yet.");
				}
				else
				{
					embed.AddInlineField("Total Pts: " + totalScore.ToFormattedString(), leftBuilder.ToString());
				}

				embed.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "exp")]
		public async Task ExpAsync(EventContext e)
		{
			//if (!await Global.RedisClient.ExistsAsync($"user:{e.Author.Id}:avatar:synced"))
			//	await Utils.SyncAvatarAsync(e.Author);

			Stream s = await client.GetStreamAsync("api/user?id=" + e.Author.Id);
			if (s == null)
			{
				e.ErrorEmbed("Image generation API did not respond. This is an issue, please report it.")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			await (e.Channel as IDiscordTextChannel).SendFileAsync(s, "exp.png", "");
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
					options.Type = LeaderboardsType.COMMANDS;
					argument = argument?.Next();
				}
				break;

				case "currency":
				case "mekos":
				case "money":
				case "bal":
				{
					options.Type = LeaderboardsType.CURRENCY;
					argument = argument?.Next();
				}
				break;

				case "rep":
				case "reputation":
				{
					options.Type = LeaderboardsType.REPUTATION;
					argument = argument?.Next();
				}
				break;

				case "pasta":
				case "pastas":
				{
					options.Type = LeaderboardsType.PASTA;
					argument = argument?.Next();
				}
				break;

				case "experience":
				case "exp":
				{
					options.Type = LeaderboardsType.EXPERIENCE;
					argument = argument?.Next();
				}
				break;

				case "guild":
				case "guilds":
				{
					options.Type = LeaderboardsType.GUILDS;
					argument = argument?.Next();
				}
				break;

				default:
				{
					options.Type = LeaderboardsType.EXPERIENCE;
				}
				break;
			}

			if (argument?.Argument.ToLower() == "local")
			{
				if (options.Type != LeaderboardsType.PASTA)
				{
					options.GuildId = e.Guild.Id;
				}
				argument = argument.Next();
			}

			if ((argument?.TakeInt() ?? 0) != 0)
			{
				options.Offset = Math.Max(0, argument.TakeInt().Value - 1) * 12;
				argument = argument?.Next();
			}

			options.Amount = 12;

			using (var context = new MikiContext())
			{
				int p = Math.Max(options.Offset - 1, 0);

				using (var api = new MikiApi(Global.Config.MikiApiBaseUrl, Global.Config.MikiApiKey))
				{
					LeaderboardsObject obj = await api.GetPagedLeaderboardsAsync(options);

					Utils.RenderLeaderboards(new EmbedBuilder(), obj.items, obj.currentPage * 12)
						.SetFooter(
							e.Locale.GetString("page_index", obj.currentPage + 1, Math.Ceiling((double)obj.totalPages / 10)),
							""
						)
						.SetAuthor(
							"Leaderboards: " + options.Type + " (click me!)",
							null,
							api.BuildLeaderboardsUrl(options)
						)
						.ToEmbed()
						.QueueToChannel(e.Channel);
				}
			}
		}

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
				IDiscordGuildUser discordUser = null;

				MarriageRepository repository = new MarriageRepository(context);

				if (arg != null)
				{
					discordUser = await arg.GetUserAsync(e.Guild);

					if (discordUser == null)
					{
						// TODO: usernullexception
						//throw new UserException(new User());
					}

					uid = discordUser.Id;
					id = uid.ToDbLong();
				}
				else
				{
					uid = e.message.Author.Id;
					discordUser = await e.Guild.GetMemberAsync(uid);

					id = uid.ToDbLong();
				}

				User account = await User.GetAsync(context, discordUser.Id.ToDbLong(), discordUser.Username);

				string icon = "";

				if (await account.IsDonatorAsync(context))
				{
					icon = "https://cdn.discordapp.com/emojis/421969679561785354.png";
				}

				if (account != null)
				{
					EmbedBuilder embed = new EmbedBuilder()
						.SetDescription(account.Title)
						.SetAuthor(e.Locale.GetString("miki_global_profile_user_header", discordUser.Username), icon, "https://patreon.com/mikibot")
						.SetThumbnail(discordUser.GetAvatarUrl());

					long serverid = e.Guild.Id.ToDbLong();

					LocalExperience localExp = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), (long)discordUser.Id, discordUser.Username);

					int rank = await localExp.GetRankAsync(context);
					int localLevel = User.CalculateLevel(localExp.Experience);
					int maxLocalExp = User.CalculateLevelExperience(localLevel);
					int minLocalExp = User.CalculateLevelExperience(localLevel - 1);

					EmojiBar expBar = new EmojiBar(maxLocalExp - minLocalExp, onBarSet, offBarSet, 6);

					string infoValue = new MessageBuilder()
						.AppendText(e.Locale.GetString("miki_module_accounts_information_level", localLevel, localExp.Experience.ToFormattedString(), maxLocalExp.ToFormattedString()))
						.AppendText(await expBar.Print(localExp.Experience - minLocalExp, e.Guild, (IDiscordGuildChannel)e.Channel))
						.AppendText(e.Locale.GetString("miki_module_accounts_information_rank", rank.ToFormattedString()))
						.AppendText("Reputation: " + account.Reputation.ToFormattedString(), MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(e.Locale.GetString("miki_generic_information"), infoValue);

					int globalLevel = User.CalculateLevel(account.Total_Experience);
					int maxGlobalExp = User.CalculateLevelExperience(globalLevel);
					int minGlobalExp = User.CalculateLevelExperience(globalLevel - 1);

					int? globalRank = await account.GetGlobalRankAsync(context);

					EmojiBar globalExpBar = new EmojiBar(maxGlobalExp - minGlobalExp, onBarSet, offBarSet, 6);

					string globalInfoValue = new MessageBuilder()
						.AppendText(e.Locale.GetString("miki_module_accounts_information_level", globalLevel.ToFormattedString(), account.Total_Experience.ToFormattedString(), maxGlobalExp.ToFormattedString()))
						.AppendText(
							await globalExpBar.Print(account.Total_Experience - minGlobalExp, e.Guild, e.Channel as IDiscordGuildChannel)
						)
						.AppendText(e.Locale.GetString("miki_module_accounts_information_rank", globalRank?.ToFormattedString() ?? "We haven't calculated your rank yet!"), MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(e.Locale.GetString("miki_generic_global_information"), globalInfoValue);
					embed.AddInlineField(e.Locale.GetString("miki_generic_mekos"), account.Currency.ToFormattedString() + "<:mekos:421972155484471296>");

					List<UserMarriedTo> Marriages = await repository.GetMarriagesAsync(id);

					Marriages.RemoveAll(x => x.Marriage.IsProposing);

					List<string> users = new List<string>();

					int maxCount = Marriages?.Count ?? 0;

					for (int i = 0; i < maxCount; i++)
					{
						users.Add((await Global.Client.Discord.GetUserAsync(Marriages[i].GetOther(id).FromDbLong())).Username);
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
						if (string.IsNullOrEmpty(marriageText))
						{
							marriageText = e.Locale.GetString("miki_placeholder_null");
						}

						embed.AddInlineField(
							e.Locale.GetString("miki_module_accounts_profile_marriedto"),
							marriageText);
					}

					Random r = new Random((int)id - 3);
					Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

					embed.SetColor(c);

					List<Achievement> allAchievements = await context.Achievements.Where(x => x.Id == id)
						.ToListAsync();

					string achievements = e.Locale.GetString("miki_placeholder_null");

					if (allAchievements != null)
					{
						if (allAchievements.Count > 0)
						{
							achievements = AchievementManager.Instance.PrintAchievements(allAchievements);
						}
					}

					embed.AddInlineField(
						e.Locale.GetString("miki_generic_achievements"),
						achievements);

					embed.SetFooter(
						e.Locale.GetString("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(),
							sw.ElapsedMilliseconds), "");

					sw.Stop();

					embed.ToEmbed().QueueToChannel(e.Channel);
				}
				else
				{
					e.ErrorEmbed(e.Locale.GetString("error_account_null"))
						.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "setbackground")]
		public async Task SetProfileBackgroundAsync(EventContext e)
		{
			int? backgroundId = e.Arguments.First().TakeInt();

			if (backgroundId == null)
				throw new ArgumentNullException("background");

			long userId = e.Author.Id.ToDbLong();

			using (var context = new MikiContext())
			{
				BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, backgroundId ?? 0);

				if (bo == null)
				{
					throw new BackgroundNotOwnedException();
				}

				ProfileVisuals v = await ProfileVisuals.GetAsync(userId, context);
				v.BackgroundId = bo.BackgroundId;
				await context.SaveChangesAsync();
			}

			e.SuccessEmbed("Successfully set background.")
				.QueueToChannel(e.Channel);
		}

		[Command(Name = "buybackground")]
		public async Task BuyProfileBackgroundAsync(EventContext e)
		{
			ArgObject arguments = e.Arguments.FirstOrDefault();

			if (arguments == null)
			{
				e.Channel.QueueMessageAsync("Enter a number after `>buybackground` to check the backgrounds! (e.g. >buybackground 1)");
			}
			else
			{
				if (arguments.TryParseInt(out int id))
				{
					if (id >= Global.Backgrounds.Backgrounds.Count || id < 0)
					{
						e.ErrorEmbed("This background does not exist!")
							.ToEmbed()
							.QueueToChannel(e.Channel);
						return;
					}

					Background background = Global.Backgrounds.Backgrounds[id];

					var embed = new EmbedBuilder()
						.SetTitle("Buy Background")
						.SetImage(background.ImageUrl);

					if (background.Price > 0)
					{
						embed.SetDescription($"This background for your profile will cost {background.Price.ToFormattedString()} mekos, Type `>buybackground {id} yes` to buy.");
					}
					else
					{
						embed.SetDescription($"This background is not for sale.");
					}

					arguments = arguments.Next();

					if (arguments?.Argument.ToLower() == "yes")
					{
						if (background.Price > 0)
						{
							using (var context = new MikiContext())
							{
								User user = await User.GetAsync(context, e.Author.Id, e.Author.Username);
								long userId = (long)e.Author.Id;

								BackgroundsOwned bo = await context.BackgroundsOwned.FindAsync(userId, background.Id);

								if (bo == null)
								{
									user.RemoveCurrency(background.Price);
									await context.BackgroundsOwned.AddAsync(new BackgroundsOwned()
									{
										UserId = e.Author.Id.ToDbLong(),
										BackgroundId = background.Id,
									});

									await context.SaveChangesAsync();

									e.SuccessEmbed("Background purchased!")
										.QueueToChannel(e.Channel);
								}
								else
								{
									throw new BackgroundOwnedException();
								}
							}
						}
					}
					else
					{
						embed.ToEmbed()
							.QueueToChannel(e.Channel);
					}
				}
			}
		}

		[Command(Name = "setbackcolor")]
		public async Task SetProfileBackColorAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await DatabaseHelpers.GetUserAsync(context, e.Author);

				var x = Regex.Matches(e.Arguments.ToString().ToUpper(), "(#)?([A-F0-9]{6})");

				if (x.Count > 0)
				{
					ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.Author.Id, context);
					var hex = x.First().Groups.Last().Value;

					visuals.BackgroundColor = hex;
					user.RemoveCurrency(250);
					await context.SaveChangesAsync();

					e.SuccessEmbed($"Your foreground color has been successfully changed to `{hex}`")
						.QueueToChannel(e.Channel);
				}
				else
				{
					new EmbedBuilder()
						.SetTitle("🖌 Setting a background color!")
						.SetDescription("Changing your background color costs 250 mekos. use `>setbackcolor (e.g. #00FF00)` to purchase")
						.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "setfrontcolor")]
		public async Task SetProfileForeColorAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User user = await DatabaseHelpers.GetUserAsync(context, e.Author);

				var x = Regex.Matches(e.Arguments.ToString().ToUpper(), "(#)?([A-F0-9]{6})");

				if (x.Count > 0)
				{
					ProfileVisuals visuals = await ProfileVisuals.GetAsync(e.Author.Id, context);
					var hex = x.First().Groups.Last().Value;

					visuals.ForegroundColor = hex;
					user.RemoveCurrency(250);
					await context.SaveChangesAsync();

					e.SuccessEmbed($"Your foreground color has been successfully changed to `{hex}`")
						.QueueToChannel(e.Channel);
				}
				else
				{
					new EmbedBuilder()
						.SetTitle("🖌 Setting a foreground color!")
						.SetDescription("Changing your foreground(text) color costs 250 mekos. use `>setfrontcolor (e.g. #00FF00)` to purchase")
						.ToEmbed().QueueToChannel(e.Channel);
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
					.SetTitle($"{e.Author.Username}'s backgrounds")
					.SetDescription(string.Join(",", backgroundsOwned.Select(x => $"`{x.BackgroundId}`")))
					.ToEmbed()
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "rep")]
		public async Task GiveReputationAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User giver = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				var repObject = await Global.RedisClient.GetAsync<ReputationObject>($"user:{giver.Id}:rep");

				if (repObject == null)
				{
					repObject = new ReputationObject()
					{
						LastReputationGiven = DateTime.Now,
						ReputationPointsLeft = 3
					};

					await Global.RedisClient.UpsertAsync(
						$"user:{giver.Id}:rep",
						repObject,
						DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
					);
				}

				ArgObject arg = e.Arguments.FirstOrDefault();

				if (arg == null)
				{
					TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

					new EmbedBuilder()
					{
						Title = e.Locale.GetString("miki_module_accounts_rep_header"),
						Description = e.Locale.GetString("miki_module_accounts_rep_description")
					}.AddInlineField(e.Locale.GetString("miki_module_accounts_rep_total_received"), giver.Reputation.ToFormattedString())
						.AddInlineField(e.Locale.GetString("miki_module_accounts_rep_reset"), pointReset.ToTimeString(e.Locale).ToString())
						.AddInlineField(e.Locale.GetString("miki_module_accounts_rep_remaining"), repObject.ReputationPointsLeft.ToString())
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}
				else
				{
					Dictionary<IDiscordUser, short> usersMentioned = new Dictionary<IDiscordUser, short>();

					EmbedBuilder embed = new EmbedBuilder();

					int totalAmountGiven = 0;
					bool mentionedSelf = false;

					while (true || totalAmountGiven <= repObject.ReputationPointsLeft)
					{
						if (arg == null)
							break;

						IDiscordUser u = await arg.GetUserAsync(e.Guild);
						short amount = 1;

						if (u == null)
							break;

						arg = arg?.Next();

						if ((arg?.TakeInt() ?? -1) != -1)
						{
							amount = (short)arg.TakeInt().Value;
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
						embed.Footer = new EmbedFooter()
						{
							Text = e.Locale.GetString("warning_mention_self"),
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
								.ToEmbed().QueueToChannel(e.Channel);
							return;
						}

						if (usersMentioned.Sum(x => x.Value) > repObject.ReputationPointsLeft)
						{
							e.ErrorEmbedResource("error_rep_limit", usersMentioned.Count, usersMentioned.Sum(x => x.Value), repObject.ReputationPointsLeft)
								.ToEmbed().QueueToChannel(e.Channel);
							return;
						}
					}

					embed.Title = (e.Locale.GetString("miki_module_accounts_rep_header"));
					embed.Description = (e.Locale.GetString("rep_success"));

					foreach (var user in usersMentioned)
					{
						User receiver = await DatabaseHelpers.GetUserAsync(context, user.Key);

						receiver.Reputation += user.Value;

						embed.AddInlineField(
							receiver.Name,
							string.Format("{0} => {1} (+{2})", (receiver.Reputation - user.Value).ToFormattedString(), receiver.Reputation.ToFormattedString(), user.Value)
						);
					}

					repObject.ReputationPointsLeft -= (short)usersMentioned.Sum(x => x.Value);

					await Global.RedisClient.UpsertAsync(
						$"user:{giver.Id}:rep",
						repObject,
						DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
					);

					embed.AddInlineField(e.Locale.GetString("miki_module_accounts_rep_points_left"), repObject.ReputationPointsLeft.ToString())
						.ToEmbed().QueueToChannel(e.Channel);

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

			EmbedBuilder embed = new EmbedBuilder();
			embed.Title = "👌 OKAY";
			embed.Description = e.Locale.GetString("sync_success", "name");
			embed.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "mekos", Aliases = new string[] { "bal", "meko" })]
		public async Task ShowMekosAsync(EventContext e)
		{
			var arg = e.Arguments.FirstOrDefault();
			IDiscordGuildUser member;

			if (arg == null)
			{
				member = await e.Guild.GetMemberAsync(e.Author.Id);
			}
			else
			{
				member = await arg.GetUserAsync(e.Guild);
			}

			using (var context = new MikiContext())
			{
				User user = await User.GetAsync(context, member.Id.ToDbLong(), member.Username);

				new EmbedBuilder()
				{
					Title = "🔸 Mekos",
					Description = e.Locale.GetString("miki_user_mekos", user.Name, user.Currency.ToFormattedString()),
					Color = new Color(1f, 0.5f, 0.7f)
				}.ToEmbed().QueueToChannel(e.Channel);

				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "give")]
		public async Task GiveMekosAsync(EventContext e)
		{
			if (e.Arguments.Count < 2)
			{
				e.ErrorEmbedResource("give_error_no_arg")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			ArgObject arg = e.Arguments.FirstOrDefault();

			IDiscordUser user = null;

			if (arg != null)
			{
				user = await arg.GetUserAsync(e.Guild);
			}

			if (user == null)
			{
				e.ErrorEmbedResource("give_error_no_mention")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			arg = arg.Next();

			int? amount = arg?.TakeInt() ?? null;

			if (amount == null)
			{
				e.ErrorEmbedResource("give_error_amount_unparsable")
					.ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			using (MikiContext context = new MikiContext())
			{
				User sender = await DatabaseHelpers.GetUserAsync(context, e.Author);
				User receiver = await DatabaseHelpers.GetUserAsync(context, user);

				if (amount.Value <= sender.Currency)
				{
					sender.RemoveCurrency(amount.Value);
					await receiver.AddCurrencyAsync(amount.Value);

					new EmbedBuilder()
					{
						Title = "🔸 transaction",
						Description = e.Locale.GetString("give_description", sender.Name, receiver.Name, amount.Value.ToFormattedString()),
						Color = new Color(255, 140, 0),
					}.ToEmbed().QueueToChannel(e.Channel);

					await context.SaveChangesAsync();
				}
				else
				{
					e.ErrorEmbedResource("user_error_insufficient_mekos")
						.ToEmbed().QueueToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "daily")]
		public async Task GetDailyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				User u = await DatabaseHelpers.GetUserAsync(context, e.Author);

				if (u == null)
				{
					e.ErrorEmbed(e.Locale.GetString("user_error_no_account"))
						.ToEmbed().QueueToChannel(e.Channel);
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
					var time = (u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.Locale);

					e.ErrorEmbed($"You already claimed your daily today! Please wait another `{time}` before using it again.")
					.AddInlineField("Appreciate Miki?", "Vote for us every day on [DiscordBots](https://discordbots.org/bot/160105994217586689/vote) to show your thanks!")
					.ToEmbed().QueueToChannel(e.Channel);
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

				await u.AddCurrencyAsync(amount);
				u.LastDailyTime = DateTime.Now;

				var embed = new EmbedBuilder()
					.SetTitle("💰 Daily")
					.SetDescription(e.Locale.GetString("daily_received", $"**{amount.ToFormattedString()}**", $"`{u.Currency.ToFormattedString()}`"))
					.SetColor(253, 216, 136);

				if (streak > 0)
				{
					embed.AddInlineField("Streak!", $"You're on a {streak.ToFormattedString()} day daily streak!");
				}

				embed.ToEmbed().QueueToChannel(e.Channel);

				await Global.RedisClient.UpsertAsync(redisKey, streak, new TimeSpan(48, 0, 0));
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
	}
}