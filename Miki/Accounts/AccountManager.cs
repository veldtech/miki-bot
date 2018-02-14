using Discord;
using Miki.Framework;
using Miki.Common;
using Miki.Common.Interfaces;
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

namespace Miki.Accounts
{
    public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordMessageChannel g, int level);

    public class AccountManager
    {
        private static AccountManager _instance = new AccountManager(Bot.Instance);
        public static AccountManager Instance => _instance;

        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;

        public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;
		private Dictionary<ulong, ExperienceAdded> experienceQueue = new Dictionary<ulong, ExperienceAdded>();
		private DateTime lastDbSync = DateTime.MinValue;

        private readonly Bot bot;

        private Dictionary<ulong, DateTime> lastTimeExpGranted = new Dictionary<ulong, DateTime>();

        private AccountManager(Bot bot)
        {
            this.bot = bot;

			OnGlobalLevelUp += async (a, e, l) =>
			{
				await Task.Yield();
				DogStatsd.Counter("levels.global", l);
			};
            OnLocalLevelUp += async (a, e, l) =>
            {
				DogStatsd.Counter("levels.local", l);

				long guildId = (long)e.Guild.Id;
                Locale locale = new Locale(e.Id);
                List<LevelRole> rolesObtained = new List<LevelRole>();

                using (var context = new MikiContext())
                {
                     rolesObtained = await context.LevelRoles
                        .Where(p => p.GuildId == guildId && p.RequiredLevel == l && p.Automatic)
                        .ToListAsync();
                }

				await a.AddRolesAsync(rolesObtained.Select(x => x.Role).ToArray());

				var setting = await Setting.GetAsync<LevelNotificationsSetting>(e.Id, DatabaseSettingId.LEVEL_NOTIFICATIONS);

				if (setting == LevelNotificationsSetting.NONE)
					return;

				if (setting == LevelNotificationsSetting.REWARDS_ONLY && rolesObtained.Count == 0)
					return;

				IDiscordEmbed embed = Utils.Embed
					.SetTitle(locale.GetString("miki_accounts_level_up_header"))
					.SetDescription(locale.GetString("miki_accounts_level_up_content", $"{a.Username}#{a.Discriminator}", l))
					.SetColor(1, 0.7f, 0.2f);
		
				if(rolesObtained.Count > 0)
				{
					embed.AddInlineField("Rewards", string.Join("\n", rolesObtained.Select(x => $"New Role: **{x.Role.Name}**")));
				}

				embed.QueueToChannel(e);
            };

            Bot.Instance.Client.GuildUpdated += Client_GuildUpdated;
            Bot.Instance.GuildJoin += Client_UserJoined;
            Bot.Instance.GuildLeave += Client_UserLeft;
        }

        public async Task CheckAsync(IDiscordMessage e)
        {
            if (e.Author.IsBot) return;

			RealtimeExperienceObject o;
			if (!await Global.redisClient.ExistsAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp"))
			{
				using (var context = new MikiContext())
				{
					LocalExperience user = await context.LocalExperience.FindAsync(e.Guild.Id.ToDbLong(), e.Author.Id.ToDbLong());

					await Global.redisClient.AddAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp", new RealtimeExperienceObject()
					{
						Experience = user.Experience,
						LastExperienceTime = DateTime.MinValue
					});
				}
			}

			o = await Global.redisClient.GetAsync<RealtimeExperienceObject>($"user:{e.Guild.Id}:{e.Author.Id}:exp");

			if (o.LastExperienceTime.AddMinutes(1) < DateTime.Now)
			{
				var ranNum = MikiRandom.Next(4, 10);
				o.Experience += ranNum;

				if (!experienceQueue.ContainsKey(e.Author.Id))
				{
					experienceQueue.Add(e.Author.Id, new ExperienceAdded()
					{
						UserId = e.Author.Id.ToDbLong(),
						GuildId = e.Guild.Id.ToDbLong(),
						Experience = ranNum,
						Name = e.Author.Username,
					});
				}
				else
				{
					experienceQueue[e.Author.Id].Experience += ranNum;
				}

				int level = User.CalculateLevel(o.Experience);

				if (User.CalculateLevel(o.Experience - ranNum) != level)
				{
					await LevelUpLocalAsync(e, level);
				}

				o.LastExperienceTime = DateTime.Now;

				await Global.redisClient.AddAsync($"user:{e.Guild.Id}:{e.Author.Id}:exp", o);
			}

			if (DateTime.Now >= lastDbSync + new TimeSpan(0, 1, 0))
			{
				Log.Message($"Applying Experience for {experienceQueue.Count} users");
				lastDbSync = DateTime.Now;
				try
				{
					await UpdateGlobalDatabase();
					await UpdateLocalDatabase();
					await UpdateGuildDatabase();
				}
				catch(Exception ex)
				{
					Log.Error(ex.Message + "\n" + ex.StackTrace);
				}
				experienceQueue.Clear();
				Log.Message($"Done Applying!");
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
            await OnLocalLevelUp.Invoke(e.Author, e.Channel, l);
        }

        public async Task LevelUpGlobalAsync(IDiscordMessage e, int l)
        {
            await OnGlobalLevelUp.Invoke(e.Author, e.Channel, l);
        }

        public async Task LogTransactionAsync(IDiscordMessage msg, User receiver, User fromUser, int amount)
        {
            await OnTransactionMade.Invoke(msg, receiver, fromUser, amount);
        }

        private async Task Client_GuildUpdated(Discord.WebSocket.SocketGuild arg1, Discord.WebSocket.SocketGuild arg2)
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

                g.UserCount = await guild.GetUserCountAsync();
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