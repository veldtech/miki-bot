using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Localization;
using Miki.Logging;
using Miki.Models;
using Miki.Modules;
using StatsdClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Localization;

namespace Miki.Accounts
{
	public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordTextChannel g, int level);

	public class AccountManager
	{
		public static AccountManager Instance { get; } = new AccountManager(MikiApp.Instance);

		public event LevelUpDelegate OnLocalLevelUp;

		public event LevelUpDelegate OnGlobalLevelUp;

		public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;

		private readonly ConcurrentDictionary<ulong, ExperienceAdded> experienceQueue = new ConcurrentDictionary<ulong, ExperienceAdded>();
		private DateTime lastDbSync = DateTime.MinValue;

		private readonly ConcurrentDictionary<ulong, DateTime> lastTimeExpGranted = new ConcurrentDictionary<ulong, DateTime>();

		private bool isSyncing = false;

		private string GetContextKey(ulong guildid, ulong userid)
			=> $"user:{guildid}:{userid}:exp";

		public AccountManager(MikiApp bot)
		{
			OnLocalLevelUp += async (a, e, l) =>
			{
				var guild = await (e as IDiscordGuildChannel).GetGuildAsync();
				long guildId = guild.Id.ToDbLong();

				List<LevelRole> rolesObtained = new List<LevelRole>();

                using (var scope = MikiApp.Instance.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetService<MikiDbContext>();
                    rolesObtained = await context.LevelRoles
						.Where(p => p.GuildId == guildId && p.RequiredLevel == l && p.Automatic)
						.ToListAsync();

					var setting = (LevelNotificationsSetting)await Setting
                        .GetAsync(context, e.Id, DatabaseSettingId.LevelUps);

					if (setting == LevelNotificationsSetting.None)
						return;

					if (setting == LevelNotificationsSetting.RewardsOnly && rolesObtained.Count == 0)
						return;

                    var pipeline = scope.ServiceProvider
                        .GetService<LocalizationPipelineStage>();

					IResourceManager instance = await pipeline
                        .GetLocaleAsync(
                            scope.ServiceProvider, 
                            (long)e.Id);

					EmbedBuilder embed = new EmbedBuilder()
					{
						Title = instance.GetString("miki_accounts_level_up_header"),
						Description = instance.GetString("miki_accounts_level_up_content", $"{a.Username}#{a.Discriminator}", l),
						Color = new Color(1, 0.7f, 0.2f)
					};

					if (rolesObtained.Count > 0)
					{
						var roles = await guild.GetRolesAsync();
						var guildUser = await guild.GetMemberAsync(a.Id);
						if (guildUser != null)
						{
							foreach (var role in rolesObtained)
							{
								var r = roles.FirstOrDefault(x => x.Id == (ulong)role.RoleId);

								if (r != null)
								{
									await guildUser.AddRoleAsync(r);
								}
							}
						}

						embed.AddInlineField("Rewards", 
							string.Join("\n", 
                                rolesObtained
                                    .Select(x => $"New Role: **{roles.FirstOrDefault(z => z.Id.ToDbLong() == x.RoleId).Name}**")));
					}

                    if (e is IDiscordTextChannel tc)
                    {
                        await embed.ToEmbed().QueueAsync(tc);
                    }
				}
		   };

            var discord = bot.GetService<DiscordClient>();

            //bot.Discord.Update += Client_GuildUpdated;
            discord.GuildMemberCreate += Client_UserJoined;
            discord.MessageCreate += CheckAsync;
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
                    var cache = MikiApp.Instance.GetService<ICacheClient>();

                    string key = GetContextKey(channel.GuildId, e.Author.Id);

                    if (lastTimeExpGranted.GetOrAdd(e.Author.Id, DateTime.Now).AddMinutes(1) < DateTime.Now)
                    {
                        int currentLocalExp = 0;
                        if (!await cache.ExistsAsync(key))
                        {
                            using(var scope = MikiApp.Instance.Services.CreateScope())
                            {
                                LocalExperience user = await LocalExperience.GetAsync(
                                    scope.ServiceProvider.GetService<DbContext>(),
                                    channel.GuildId,
                                    e.Author.Id,
                                    e.Author.Username
                                );

                                await cache.UpsertAsync(key, user.Experience);
                                currentLocalExp = user.Experience;
                            }
                        }
                        else
                        {
                            currentLocalExp = await cache.GetAsync<int>(key);
                        }

                        var bonusExp = MikiRandom.Next(1, 4);
                        currentLocalExp += bonusExp;

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

                        int level = User.CalculateLevel(currentLocalExp);

                        if (User.CalculateLevel(currentLocalExp - bonusExp) != level)
                        {
                            await LevelUpLocalAsync(e, level);
                        }

                        lastTimeExpGranted.AddOrUpdate(e.Author.Id, DateTime.Now, (x, d) => DateTime.Now);

                        await cache.UpsertAsync(key, currentLocalExp);
                    }
                }

				if (DateTime.Now >= lastDbSync + new TimeSpan(0, 1, 0))
				{
					isSyncing = true;
					Log.Message($"Applying Experience for {experienceQueue.Count} users");
					lastDbSync = DateTime.Now;

					try
					{
                        using (var scope = MikiApp.Instance.Services.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetService<DbContext>();
                            await UpdateGlobalDatabase(context);
                            await UpdateLocalDatabase(context);
                            await UpdateGuildDatabase(context);
                            await context.SaveChangesAsync();
                        }
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

        public async Task UpdateGlobalDatabase(DbContext context)
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

            await context.Database.ExecuteSqlCommandAsync(query, userParameters.ToArray());
        }

        public async Task UpdateLocalDatabase(DbContext context)
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


            await context.Database.ExecuteSqlCommandAsync(query);
        }

        public async Task UpdateGuildDatabase(DbContext context)
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

            await context.Database.ExecuteSqlCommandAsync(query);
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
                using(var scope = MikiApp.Instance.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetService<MikiDbContext>();
					GuildUser g = await context.GuildUsers.FindAsync(arg1.Id.ToDbLong());
					g.Name = arg2.Name;
					await context.SaveChangesAsync();
				}
			}
		}

		private async Task Client_UserJoined(IDiscordGuildUser arg)
		{
			await UpdateGuildUserCountAsync(await arg.GetGuildAsync());
		}

		private async Task UpdateGuildUserCountAsync(IDiscordGuild guild)
		{
            using (var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<MikiDbContext>();
                GuildUser g = await context.GuildUsers.FindAsync(guild.Id.ToDbLong());
				g.UserCount = guild.MemberCount;
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