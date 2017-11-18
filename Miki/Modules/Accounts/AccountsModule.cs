	#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Discord;
using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Builders;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Accounts;
using Miki.Accounts.Achievements;
using Miki.Languages;
using Miki.Models;
using Miki.Modules.Accounts.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Miki.Modules.AccountsModule
{
	[Module("Accounts")]
	public class AccountsModule
	{
		public AccountsModule(RuntimeModule module)
		{
			AccountManager.Instance.OnLocalLevelUp += async (a, g, l) =>
			{
				using (var context = new MikiContext())
				{
					long guildId = g.Id.ToDbLong();
					List<LevelRole> rolesObtained = context.LevelRoles.AsNoTracking()
						.Where(p => p.GuildId == guildId && p.RequiredLevel == l)
						.ToList();

					IDiscordUser u = await g.Guild.GetUserAsync(a.Id.FromDbLong());
					List<IDiscordRole> rolesGiven = new List<IDiscordRole>();

					if (rolesObtained == null)
					{
						return;
					}

					foreach (LevelRole r in rolesObtained)
					{
						rolesGiven.Add(r.Role);
					}

					if (rolesGiven.Count > 0)
					{
						await u.AddRolesAsync(rolesGiven);
					}
				}
			};

			new AchievementsService()
				.Install(module);

			new ExperienceTrackerService()
				.Install(module);
		}

		[Command(Name = "leaderboards", Aliases = new[] { "lb", "leaderboard", "top" })]
		public async Task LeaderboardsAsync(EventContext e)
		{
			LeaderboardOptions options = new LeaderboardOptions();	
			options.pageNumber = 0;
		
			string[] args = e.arguments.Split(' ');

			if(e.message.MentionedUserIds.Count() > 0)
			{
				options.mentionedUserId = e.message.MentionedUserIds.First();
			}
			else
			{
				string toParse = args.Count() > 1 ? args[1] : args[0];

				if(toParse.Contains("me") || toParse.Contains("self"))
				{
					options.mentionedUserId = e.message.Author.Id;
				}
				else if(toParse.Contains( "#" ))
				{
					toParse = toParse.Substring(1);
					if(int.TryParse(toParse, out int parsedNumber))
					{
						options.pageNumber = (int)Math.Ceiling(parsedNumber / 12.0);
					}
				}
				else
				{
					int.TryParse(toParse, out options.pageNumber);
				}
			}

			switch(args[0].ToLower())
			{
				case "local":
				case "server":
				case "guild":
					{
						options.type = LeaderboardsType.LocalExperience;
					}
					break;

				case "commands":
				case "cmds":
					{
						options.type = LeaderboardsType.Commands;
					}
					break;

				case "currency":
				case "mekos":
				case "money":
				case "bal":
					{
						options.type = LeaderboardsType.Currency;
					}
					break;

				case "rep":
				case "reputation":
					{
						options.type = LeaderboardsType.Reputation;
					}
					break;

				default:
					{
						options.type = LeaderboardsType.Experience;
					}
					break;
			}

			await ShowLeaderboardsAsync(e.message, options);
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

				if (e.message.MentionedUserIds.Any())
				{
					uid = e.message.MentionedUserIds.First();
					id = uid.ToDbLong();
				}
				else
				{
					uid = e.message.Author.Id;
					id = uid.ToDbLong();
				}

				User account = await context.Users.FindAsync(id);
				Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
				IDiscordUser discordUser = await e.Guild.GetUserAsync(uid);

				if (account != null)
				{
					IDiscordEmbed embed = Utils.Embed
						.SetDescription(account.Title)
						.SetAuthor(locale.GetString("miki_global_profile_user_header", account.Name),
							"http://veld.one/assets/profile-icon.png", "https://patreon.com/mikibot")
						.SetThumbnailUrl(discordUser.AvatarUrl);

					long serverid = e.Guild.Id.ToDbLong();

					LocalExperience localExp = await context.Experience.FindAsync(serverid, id);
					int globalExp = account.Total_Experience;

					int rank = await account.GetLocalRank(e.Guild.Id);

					EmojiBarSet onBarSet = new EmojiBarSet("<:mbaronright:334479818924228608>",
						"<:mbaronmid:334479818848468992>", "<:mbaronleft:334479819003789312>");
					EmojiBarSet offBarSet = new EmojiBarSet("<:mbaroffright:334479818714513430>",
						"<:mbaroffmid:334479818504536066>", "<:mbaroffleft:334479818949394442>");

					EmojiBar expBar = new EmojiBar(User.CalculateMaxExperience(localExp.Experience), onBarSet,
						offBarSet, 6);

					string infoValue = new MessageBuilder()
						.AppendText(locale.GetString("miki_module_accounts_information_level",
							User.CalculateLevel(localExp.Experience), localExp.Experience,
							User.CalculateMaxExperience(localExp.Experience)))
						.AppendText(await expBar.Print(localExp.Experience, e.Channel))
						.AppendText(locale.GetString("miki_module_accounts_information_rank", rank))
						.AppendText("Reputation: " + account.Reputation, MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(locale.GetString("miki_generic_information"), infoValue);

					int globalLevel = User.CalculateLevel(account.Total_Experience);
					int globalRank = User.CalculateMaxExperience(account.Total_Experience);

					EmojiBar globalExpBar = new EmojiBar(User.CalculateMaxExperience(account.Total_Experience),
						onBarSet, offBarSet, 6);

					string globalInfoValue = new MessageBuilder()
						.AppendText(locale.GetString("miki_module_accounts_information_level", globalLevel,
							account.Total_Experience, globalRank))
						.AppendText(await globalExpBar.Print(account.Total_Experience, e.Channel))
						.AppendText(
							locale.GetString("miki_module_accounts_information_rank",
								await account.GetGlobalRankAsync()), MessageFormatting.Plain, false)
						.Build();

					embed.AddInlineField(locale.GetString("miki_generic_global_information"), globalInfoValue);

					embed.AddInlineField(locale.GetString("miki_generic_mekos"), account.Currency + "🔸");

					List<Marriage> marriages = Marriage.GetMarriages(context, id);

					marriages = marriages.OrderBy(mar => mar.TimeOfMarriage).ToList();

					List<User> users = new List<User>();

					int maxCount = marriages.Count;

					for (int i = 0; i < maxCount; i++)
					{
						users.Add(await context.Users.FindAsync(marriages[i].GetOther(id)));
					}

					if (marriages.Count > 0)
					{
						List<string> marriageStrings = new List<string>();

						for (int i = 0; i < maxCount; i++)
						{
							if (marriages[i].GetOther(id) != 0)
							{
								marriageStrings.Add("💕 " + users[i].Name + " (_" +
													marriages[i].TimeOfMarriage.ToShortDateString() + "_)");
							}
						}

						embed.AddInlineField(
							locale.GetString("miki_module_accounts_profile_marriedto"),
							string.Join("\n", marriageStrings));
					}

					Random r = new Random((int)id - 3);

					embed.Color = new IA.SDK.Color((float)r.NextDouble(), (float)r.NextDouble(),
						(float)r.NextDouble());

					List<CommandUsage> List = context.CommandUsages.Where(c => c.UserId == id)
						.OrderByDescending(c => c.Amount).ToList();
					string favCommand = (List.Count > 0) ? List[0].Name + " (" + List[0].Amount + ")" : "none (yet!)";

					embed.AddInlineField(locale.GetString("miki_module_accounts_profile_favourite_command"),
						favCommand);

					string achievements =
						AchievementManager.Instance.PrintAchievements(context, account.Id.FromDbLong());

					embed.AddInlineField(
						locale.GetString("miki_generic_achievements"),
						achievements != "" ? achievements : locale.GetString("miki_placeholder_null"));

					embed.AddInlineField(locale.GetString("miki_module_accounts_profile_url"),
						"http://miki.veld.one/profile/" + account.Id);

					embed.SetFooter(
						locale.GetString("miki_module_accounts_profile_footer", account.DateCreated.ToShortDateString(),
							sw.ElapsedMilliseconds), "");

					sw.Stop();

					await embed.SendToChannel(e.Channel);
				}
				else
				{
					await Utils.ErrorEmbed(locale, locale.GetString("miki_module_accounts_error_null"))
						.SendToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "rep")]
		public async Task GiveReputationAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
				User giver = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				List<ulong> mentionedUsers = e.message.MentionedUserIds.ToList();
				string[] args = e.arguments.Split(' ');
				short repAmount = 1;

				bool mentionedSelf = mentionedUsers.RemoveAll(x => x == e.Author.Id) > 0;

				if (giver.LastReputationGiven.Day != DateTime.Now.Day)
				{
					giver.ReputationPointsLeft = 3;
					giver.LastReputationGiven = DateTime.Now;
				}

				IDiscordEmbed embed = Utils.Embed;

				if(mentionedSelf)
				{
					embed.SetFooter(e.GetResource("warning_mention_self"), "");
				}

				if (mentionedUsers.Count == 0)
				{
					TimeSpan pointReset = (DateTime.Now.AddDays(1).Date - DateTime.Now);

					await embed
						.SetTitle(locale.GetString("miki_module_accounts_rep_header"))
						.SetDescription(locale.GetString("miki_module_accounts_rep_description"))
						.AddInlineField(locale.GetString("miki_module_accounts_rep_total_received"), giver.Reputation.ToString())
						.AddInlineField(locale.GetString("miki_module_accounts_rep_reset"), pointReset.ToTimeString(e.Channel.GetLocale()))
						.AddInlineField(locale.GetString("miki_module_accounts_rep_remaining"), giver.ReputationPointsLeft)
						.SendToChannel(e.Channel);
					return;
				}
				else
				{
					if (args.Length > 1)
					{
						if (Utils.IsAll(args[args.Length - 1], e.Channel.GetLocale()))
						{
							repAmount = giver.ReputationPointsLeft;
						}
						else if (short.TryParse(args[1], out short x))
						{
							repAmount = x;
						}					
					}

					if (repAmount <= 0)
					{
						await e.ErrorEmbed(locale.GetString("miki_module_accounts_rep_error_zero"))
							.SendToChannel(e.Channel);
						return;
					}

					if(mentionedUsers.Count * repAmount > giver.ReputationPointsLeft)
					{
						await e.ErrorEmbed("You can not give {0} user(s) {1} reputation point(s) while you only have {2} points left.",
							mentionedUsers.Count, repAmount, giver.ReputationPointsLeft)
							.SendToChannel(e.Channel);
						return;
					}
				}

				embed.SetTitle(locale.GetString("miki_module_accounts_rep_header"))
					.SetDescription("You've successfully given reputation");

				foreach (ulong user in mentionedUsers)
				{
					User receiver = await context.Users.FindAsync(user.ToDbLong());
					if (receiver == null)
					{
						IDiscordUser u = await e.Guild.GetUserAsync(user);
						receiver = await User.CreateAsync(u);
					}

					receiver.Reputation += repAmount;

					embed.AddInlineField(receiver.Name, string.Format("{0} => {1} (+{2})", receiver.Reputation - repAmount, receiver.Reputation, repAmount));
				}

				giver.ReputationPointsLeft -= (short)(repAmount * mentionedUsers.Count);

				await embed
					.AddInlineField(locale.GetString("miki_module_accounts_rep_points_left"), giver.ReputationPointsLeft)
					.SendToChannel(e.Channel);

				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "syncavatar")]
		public async Task SyncAvatarAsync(EventContext e)
		{
			string localFilename = @"c:\inetpub\miki.veld.one\assets\img\user\" + e.Author.Id + ".png";

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(e.Author.GetAvatarUrl());
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			// Check that the remote file was found. The ContentType
			// check is performed since a request for a non-existent
			// image file might be redirected to a 404-page, which would
			// yield the StatusCode "OK", even though the image was not
			// found.
			if ((response.StatusCode == HttpStatusCode.OK ||
				 response.StatusCode == HttpStatusCode.Moved ||
				 response.StatusCode == HttpStatusCode.Redirect) &&
				response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
			{
				// if the remote file was found, download oit
				using (Stream inputStream = response.GetResponseStream())
				using (Stream outputStream = File.OpenWrite(localFilename))
				{
					byte[] buffer = new byte[4096];
					int bytesRead;
					do
					{
						bytesRead = inputStream.Read(buffer, 0, buffer.Length);
						outputStream.Write(buffer, 0, bytesRead);
					} while (bytesRead != 0);
				}
			}

			using (var context = new MikiContext())
			{
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				if (user == null)
				{
					return;
				}
				user.AvatarUrl = e.Author.Id.ToString();
				await context.SaveChangesAsync();
			}

			IDiscordEmbed embed = Utils.Embed;
			embed.Title = "👌 OKAY";
			embed.Description = "I've synchronized your current avatar to Miki's database!";
			await embed.SendToChannel(e.Channel);
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

			IDiscordEmbed embed = Utils.Embed;
			embed.Title = "👌 OKAY";
			embed.Description = "I've synchronized your current name to Miki's database!";
			await embed.SendToChannel(e.Channel);
		}

		[Command(Name = "mekos", Aliases = new string[] { "bal", "meko" })]
		public async Task ShowMekosAsync(EventContext e)
		{
			ulong targetId = e.message.MentionedUserIds.Count > 0 ? e.message.MentionedUserIds.First() : 0;

			if (e.message.MentionedUserIds.Count > 0)
			{
				if (targetId == 0)
				{
					await e.ErrorEmbed(e.GetResource("miki_module_accounts_mekos_no_user")).SendToChannel(e.Channel);
					return;
				}
				IDiscordUser userCheck = await e.Guild.GetUserAsync(targetId);
				if (userCheck.IsBot)
				{
					await e.ErrorEmbed(e.GetResource("miki_module_accounts_mekos_bot")).SendToChannel(e.Channel);
					return;
				}
			}

			using (var context = new MikiContext())
			{
				User user = await context.Users.FindAsync(targetId != 0 ? (long)targetId : e.Author.Id.ToDbLong());

				IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
				embed.Title = "🔸 Mekos";
				embed.Description = e.GetResource("miki_user_mekos", user.Name, user.Currency);
				embed.Color = new IA.SDK.Color(1f, 0.5f, 0.7f);

				await embed.SendToChannel(e.Channel);
			}
		}

		[Command(Name = "give")]
		public async Task GiveMekosAsync(EventContext e)
		{
			Locale locale = Locale.GetEntity(e.Guild.Id);

			string[] arguments = e.arguments.Split(' ');

			if (arguments.Length < 2)
			{
				await Utils.ErrorEmbed(locale, e.GetResource("give_error_no_arg")).SendToChannel(e.Channel);
				return;
			}

			if (e.message.MentionedUserIds.Count <= 0)
			{
				await Utils.ErrorEmbed(locale, e.GetResource("give_error_no_mention")).SendToChannel(e.Channel);
				return;
			}

			if (!int.TryParse(arguments[1], out int goldSent))
			{
				await Utils.ErrorEmbed(locale, e.GetResource("give_error_amount_unparsable")).SendToChannel(e.Channel);
				return;
			}

			if (goldSent > 999999)
			{
				await Utils.ErrorEmbed(locale, e.GetResource("give_error_max_mekos")).SendToChannel(e.Channel);
				return;
			}

			if (goldSent <= 0)
			{
				await Utils.ErrorEmbed(locale, e.GetResource("give_error_min_mekos")).SendToChannel(e.Channel);
				return;
			}

			using (MikiContext context = new MikiContext())
			{
				User sender = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (sender == null)
				{
					// HOW THE FUCK?!
					return;
				}

				User receiver = await context.Users.FindAsync(e.message.MentionedUserIds.First().ToDbLong());

				if (receiver == null)
				{
					await Utils.ErrorEmbed(locale, e.GetResource("user_error_no_account"))
						.SendToChannel(e.Channel);
					return;
				}

				if (goldSent <= sender.Currency)
				{
					await receiver.AddCurrencyAsync(goldSent, e.Channel, sender);
					await sender.AddCurrencyAsync(-goldSent, e.Channel, sender);

					IDiscordEmbed em = Utils.Embed;
					em.Title = "🔸 transaction";
					em.Description = e.GetResource("give_description", sender.Name, receiver.Name, goldSent);

					em.Color = new IA.SDK.Color(255, 140, 0);

					await context.SaveChangesAsync();
					await em.SendToChannel(e.Channel);
				}
				else
				{
					await Utils.ErrorEmbed(locale, e.GetResource("user_error_insufficient_mekos"))
						.SendToChannel(e.Channel);
				}
			}
		}

		[Command(Name = "daily")]
		public async Task GetDailyAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

				User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (u == null)
				{
					await Utils.ErrorEmbed(locale, e.GetResource("user_error_no_account"))
						.SendToChannel(e.Channel);
					return;
				}

				int dailyAmount = 100;

				if (u.IsDonator(context))
				{
					dailyAmount *= 2;
				}

				if (u.LastDailyTime.AddHours(23) >= DateTime.Now)
				{
					await e.Channel.SendMessage(
						$"You already claimed your daily today! Please wait another `{(u.LastDailyTime.AddHours(23) - DateTime.Now).ToTimeString(e.Channel.GetLocale())}` before using it again.");
					return;
				}

				await u.AddCurrencyAsync(dailyAmount, e.Channel);
				u.LastDailyTime = DateTime.Now;

				await Utils.Embed
					.SetTitle(locale.GetString("Daily"))
					.SetDescription($"Received **{dailyAmount}** Mekos! You now have `{u.Currency}` Mekos")
					.SendToChannel(e.Channel);

				await context.SaveChangesAsync();
			}
		}

		[Command(Name = "setrolelevel")]
		public async Task SetRoleLevelAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

				List<string> allArgs = new List<string>();
				allArgs.AddRange(e.arguments.Split(' '));
				if (allArgs.Count >= 2)
				{
					int levelrequirement = int.Parse(allArgs[allArgs.Count - 1]);
					allArgs.RemoveAt(allArgs.Count - 1);
					IDiscordRole role = e.Guild.Roles
						.Find(r => r.Name.ToLower() == string.Join(" ", allArgs).TrimEnd(' ').TrimStart(' ').ToLower());

					if (role == null)
					{
						await e.ErrorEmbed(e.GetResource("error_role_not_found"))
							.SendToChannel(e.Channel);
						return;
					}

					LevelRole lr = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
					if (lr == null)
					{
						lr = context.LevelRoles.Add(new LevelRole()
						{
							GuildId = e.Guild.Id.ToDbLong(),
							RoleId = role.Id.ToDbLong(),
							RequiredLevel = levelrequirement
						});

						IDiscordEmbed embed = Utils.Embed;
						embed.Title = "Added Role!";
						embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";

						if (!e.CurrentUser.HasPermissions(e.Channel, DiscordGuildPermission.ManageRoles))
						{
							embed.AddInlineField(e.GetResource("miki_warning"), e.GetResource("setrolelevel_error_no_permissions", $"`{e.GetResource("permission_manage_roles")}`"));
						}

						await embed.SendToChannel(e.Channel);
					}
					else
					{
						lr.RequiredLevel = levelrequirement;

						IDiscordEmbed embed = Utils.Embed;
						embed.Title = "Updated Role!";
						embed.Description = $"I'll give someone the role {role.Name} when he/she reaches level {levelrequirement}!";
						await embed.SendToChannel(e.Channel);
					}
					await context.SaveChangesAsync();
				}
				else
				{
					await Utils.ErrorEmbed(locale, "Make sure to fill out both the role and the level when creating this!")
						.SendToChannel(e.Channel);
				}
			}
		}

		public async Task ShowLeaderboardsAsync(IDiscordMessage mContext, LeaderboardOptions leaderboardOptions)
		{
			using (var context = new MikiContext())
			{
				Locale locale = Locale.GetEntity(mContext.Channel.Id.ToDbLong());

				int p = Math.Max(leaderboardOptions.pageNumber - 1, 0);

				IDiscordEmbed embed = Utils.Embed
					.SetColor(1.0f, 0.6f, 0.4f)
					.SetFooter(locale.GetString("page_index", p + 1, Math.Ceiling(context.Users.Count() / 12f)), "");

				switch(leaderboardOptions.type)
				{
					case LeaderboardsType.Commands:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_commands_header");
							if(leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								p = (int)Math.Floor( context.Users.OrderByDescending(x => x.Total_Commands).ToList().FindIndex(x => x.Id == mentionedId) / 12.0 );
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Total_Commands)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for(int i = 0; i < output.Count; i++)
							{
								string nameToOutput = leaderboardOptions.mentionedUserId != 0 ? string.Join("", output[i].Name.Take(16)) : "~" + string.Join("", output[i].Name.Take(16)) + "~";
								embed.AddInlineField($"#{i + (12 * p) + 1}: {nameToOutput}", $"{output[i].Total_Commands} commands used!");
							}
						}
						break;

					case LeaderboardsType.Currency:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_mekos_header");
							if(leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								p = (int)Math.Floor(context.Users.OrderByDescending(x => x.Total_Commands).ToList().FindIndex(x => x.Id == mentionedId) / 12.0);
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Currency)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for(int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Currency} mekos!");
							}
						}
						break;

					case LeaderboardsType.LocalExperience:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_local_header");
							long guildId = mContext.Guild.Id.ToDbLong();
							if(leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								p = (int)Math.Floor(context.Experience.Where(x => x.ServerId == guildId).OrderByDescending(x => x.Experience).ToList().FindIndex(x => x.UserId == mentionedId) / 12.0);
							}
							List<LocalExperience> output = await context.Experience
								.Where(x => x.ServerId == guildId)
								.OrderByDescending(x => x.Experience)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							int amountOfUsers = await context.Experience.Where(x => x.ServerId == guildId).CountAsync();

							List<User> users = new List<User>();

							for(int i = 0; i < output.Count; i++)
							{
								users.Add(await context.Users.FindAsync(output[i].UserId));
							}

							for(int i = 0; i < users.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1} : {string.Join("", users[i].Name.Take(16))}",
									$"{output[i].Experience} experience!");
							}
						}
						break;

					case LeaderboardsType.Experience:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_header");
							if(leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								p = (int)Math.Floor(context.Users.OrderByDescending(x => x.Total_Experience).ToList().FindIndex(x => x.Id == mentionedId) / 12.0);
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Total_Experience)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for(int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Total_Experience} experience!");
							}
						}
						break;

					case LeaderboardsType.Reputation:
						{
							embed.Title = locale.GetString("miki_module_accounts_leaderboards_reputation_header");
							if(leaderboardOptions.mentionedUserId != 0)
							{
								long mentionedId = leaderboardOptions.mentionedUserId.ToDbLong();
								p = (int)Math.Floor(context.Users.OrderByDescending(x => x.Reputation).ToList().FindIndex(x => x.Id == mentionedId) / 12.0);
							}
							List<User> output = await context.Users
								.OrderByDescending(x => x.Reputation)
								.Skip(12 * p)
								.Take(12)
								.ToListAsync();

							for(int i = 0; i < output.Count; i++)
							{
								embed.AddInlineField($"#{i + (12 * p) + 1}: {string.Join("", output[i].Name.Take(16))}",
									$"{output[i].Reputation} reputation!");
							}
						}
						break;
				}

				await embed.SendToChannel(mContext.Channel);
			}
		}
	}

	public enum LeaderboardsType
	{
		LocalExperience,
		Experience,
		Commands,
		Currency,
		Reputation
	}

	public struct LeaderboardOptions
	{
		public LeaderboardsType type;
		public ulong mentionedUserId;
		public int pageNumber;
	}
}