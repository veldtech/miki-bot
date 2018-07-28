using Miki.Framework;
using Miki.Common;
using Microsoft.EntityFrameworkCore;
using Miki.Languages;
using Miki.Models;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.Modules;
using System.Collections.Concurrent;
using Miki.Framework.Extension;
using Miki.Framework.Languages;
using Miki.Logging;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Discord;

namespace Miki.Accounts
{
    public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordChannel g, int level);

    public class AccountManager
    {
        private static AccountManager _instance = new AccountManager(Bot.Instance);
        public static AccountManager Instance => _instance;

        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;

        public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;
		private ConcurrentDictionary<ulong, ExperienceAdded> experienceQueue = new ConcurrentDictionary<ulong, ExperienceAdded>();
		private DateTime lastDbSync = DateTime.MinValue;

        private ConcurrentDictionary<ulong, DateTime> lastTimeExpGranted = new ConcurrentDictionary<ulong, DateTime>();

		private bool isSyncing = false;

		string GetContextKey(ulong guildid, ulong userid)
			=> $"user:{guildid}:{userid}:exp";


		public AccountManager(Bot bot)
        {
			OnGlobalLevelUp += (a, e, l) =>
			{
				DogStatsd.Counter("levels.global", l);
				return Task.CompletedTask;
			};
			OnLocalLevelUp += async (a, e, l) =>
		   {
			   DogStatsd.Counter("levels.local", l);

			   long guildId = (e as IDiscordGuildChannel).GuildId.ToDbLong();

			   List<LevelRole> rolesObtained = new List<LevelRole>();

			   using (var context = new MikiContext())
			   {
				   rolesObtained = await context.LevelRoles
					  .Where(p => p.GuildId == guildId && p.RequiredLevel == l && p.Automatic)
					  .ToListAsync();
			   }

			  // await (a as IDiscordGuildUser).AddRolesAsync(rolesObtained.Select(x => x.Role).ToArray());

			   var setting = await Setting.GetAsync<LevelNotificationsSetting>(e.Id, DatabaseSettingId.LEVEL_NOTIFICATIONS);

			   if (setting == LevelNotificationsSetting.NONE)
				   return;

			   if (setting == LevelNotificationsSetting.REWARDS_ONLY && rolesObtained.Count == 0)
				   return;

			   EmbedBuilder embed = new EmbedBuilder()
			   {
				   Title = await Locale.GetStringAsync(e.Id, "miki_accounts_level_up_header"),
				   Description = await Locale.GetStringAsync(e.Id, "miki_accounts_level_up_content", $"{a.Username}#{a.Discriminator}", l),
				   Color = new Color(1, 0.7f, 0.2f)
			   };

			   if (rolesObtained.Count > 0)
			   {
				   embed.AddInlineField("Rewards", string.Join("\n", rolesObtained.Select(x => $"New Role: **{x.Role.Name}**")));
			   }

			   embed.ToEmbed().QueueToChannel(e);
		   };

			//	bot.Client.GuildUpdated += Client_GuildUpdated;
			//	bot.Client.JoinedGuild   += Client_UserJoined;\
			//	bot.Client.LeftGuild  += Client_UserLeft;
			bot.Client.MessageCreate += CheckAsync;
		}

		public async Task CheckAsync(IDiscordMessage e)
		{
			if (e.Author.IsBot)
			{
				return;
			}

			if (isSyncing)
			{
				return;
			}

			try
			{
				if (await e.GetChannelAsync() is IDiscordGuildChannel channel)
				{
					string key = GetContextKey(channel.GuildId, e.Author.Id);

					if (lastTimeExpGranted.GetOrAdd(e.Author.Id, DateTime.Now).AddMinutes(1) < DateTime.Now)
					{
						int currentExp = 0;
						if (!await Global.RedisClient.ExistsAsync(key))
						{
							using (var context = new MikiContext())
							{
								LocalExperience user = await LocalExperience.GetAsync(
									context,
									(long)channel.GuildId,
									e.Author
								);

								await Global.RedisClient.UpsertAsync(key, user.Experience);
								currentExp = user.Experience;
							}
						}
						else
						{
							currentExp = await Global.RedisClient.GetAsync<int>(key);
						}

						var bonusExp = MikiRandom.Next(1, 4);
						currentExp += bonusExp;

						if (!experienceQueue.ContainsKey(e.Author.Id))
						{
							var expObject = new ExperienceAdded()
							{
								UserId = e.Author.Id.ToDbLong(),
								GuildId = channel.GuildId.ToDbLong(),
								Experience = bonusExp,
								Name = e.Author.Username,
							};

							experienceQueue.AddOrUpdate(e.Author.Id, expObject, (u, eo) =>
							{
								eo.Experience += expObject.Experience;
								return eo;
							});
						}
						else
						{
							experienceQueue[e.Author.Id].Experience += bonusExp;
						}

						int level = User.CalculateLevel(currentExp);

						if (User.CalculateLevel(currentExp - bonusExp) != level)
						{
							await LevelUpLocalAsync(e, level);
						}

						lastTimeExpGranted.AddOrUpdate(e.Author.Id, DateTime.Now, (x, d) => DateTime.Now);

						await Global.RedisClient.UpsertAsync(key, currentExp);
					}
				}

				if (DateTime.Now >= lastDbSync + new TimeSpan(0, 1, 0))
				{
					isSyncing = true;
					Log.Message($"Applying Experience for {experienceQueue.Count} users");
					lastDbSync = DateTime.Now;

					try
					{
						await UpdateGlobalDatabase();
						await UpdateLocalDatabase();
						await UpdateGuildDatabase();
					}
					catch (Exception ex)
					{
						Log.Error(ex.Message + "\n" + ex.StackTrace);
					}
					finally
					{
						experienceQueue.Clear();
						isSyncing = false;
					}
					Log.Message($"Done Applying!");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.ToString());
			}
		}

		public async Task UpdateGlobalDatabase()
		{
			if (experienceQueue.Count == 0)
				return;


			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, name, experience) as (values";

			List<string> userParameters = new List<string>();

			for (int i = 0; i < experienceQueue.Values.Count; i++)
			{
				userQuery.Add($"({experienceQueue.Values.ElementAt(i).UserId}, @p{i}, {experienceQueue.Values.ElementAt(i).Experience})");
				userParameters.Add(experienceQueue.Values.ElementAt(i).Name ?? "name failed to set?");
			}

			string y = $"),upsert as ( update \"dbo\".\"Users\" m set \"Total_Experience\" = \"Total_Experience\" + nv.experience FROM new_values nv WHERE m.\"Id\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"Users\"(\"Id\", \"Name\", \"Total_Experience\") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up.\"Id\" = new_values.id);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				await context.Database.ExecuteSqlCommandAsync(query, userParameters.ToArray());
				await context.SaveChangesAsync();
			}
		}

		public async Task UpdateLocalDatabase()
		{
			if (experienceQueue.Count == 0)
				return;

			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, serverid, experience) as (values ";

			for (int i = 0; i < experienceQueue.Values.Count; i++)
			{
				userQuery.Add($"({experienceQueue.Values.ElementAt(i).UserId}, {experienceQueue.Values.ElementAt(i).GuildId}, {experienceQueue.Values.ElementAt(i).Experience})");
			}

			string y = $"),upsert as(update \"dbo\".\"LocalExperience\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"UserId\" = nv.id AND m.\"ServerId\" = nv.serverid RETURNING m.*) INSERT INTO \"dbo\".\"LocalExperience\"(\"UserId\", \"ServerId\", \"Experience\") SELECT id, serverid, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"UserId\" = new_values.id AND up.\"ServerId\" = new_values.serverid);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				await context.Database.ExecuteSqlCommandAsync(query);
				await context.SaveChangesAsync();
			}
		}

		public async Task UpdateGuildDatabase()
		{
			if (experienceQueue.Count == 0)
				return;

			List<string> userQuery = new List<string>();
			string x = "WITH new_values (id, experience) as (values ";

			for (int i = 0; i < experienceQueue.Values.Count; i++)
			{
				userQuery.Add($"({experienceQueue.Values.ElementAt(i).GuildId}, {experienceQueue.Values.ElementAt(i).Experience})");
			}

			string y = $"),upsert as(update \"dbo\".\"GuildUsers\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"EntityId\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"GuildUsers\"(\"EntityId\", \"Experience\") SELECT id, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"EntityId\" = new_values.id);";

			string query = x + string.Join(",", userQuery) + y;

			using (var context = new MikiContext())
			{
				await context.Database.ExecuteSqlCommandAsync(query);
				await context.SaveChangesAsync();
			}
		}

		#region Events
		public async Task LevelUpLocalAsync(IDiscordMessage e, int l)
        {
            await OnLocalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
        }

        public async Task LevelUpGlobalAsync(IDiscordMessage e, int l)
        {
            await OnGlobalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
        }

        public async Task LogTransactionAsync(IDiscordMessage msg, User receiver, User fromUser, int amount)
        {
            await OnTransactionMade.Invoke(msg, receiver, fromUser, amount);
        }

        private async Task Client_GuildUpdated(IDiscordGuild arg1, IDiscordGuild arg2)
        {
            if (arg1.Name != arg2.Name)
            {
                using (MikiContext context = new MikiContext())
                {
                    GuildUser g = await context.GuildUsers.FindAsync(arg1.Id.ToDbLong());
                    g.Name = arg2.Name;
                    await context.SaveChangesAsync();
                }
            }
        }

        private async Task Client_UserLeft(IDiscordGuild arg)
        {
            await UpdateGuildUserCountAsync(arg);
        }

        private async Task Client_UserJoined(IDiscordGuild arg)
        {
            await UpdateGuildUserCountAsync(arg);
        }

        private async Task UpdateGuildUserCountAsync(IDiscordGuild guild)
        {
            using (MikiContext context = new MikiContext())
            {
				GuildUser g = await GuildUser.GetAsync(context, guild);

                g.UserCount = (await guild.GetUsersAsync()).Count;
                await context.SaveChangesAsync();
            }
        }
        #endregion Events
    }

	public class ExperienceAdded
	{
		public long GuildId;
		public long UserId;
		public int Experience;
		public string Name;
	}
}