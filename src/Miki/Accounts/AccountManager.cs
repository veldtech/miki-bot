using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord.Common;
using Miki.Discord.Common.Models;
using Miki.Framework;
using Miki.Logging;
using Miki.Utility;
using Sentry;

namespace Miki.Accounts
{
    public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordTextChannel g, int level);

    public class AccountService
    {
        private readonly MikiApp app;
        private readonly ISentryClient sentryClient;
        private readonly ICacheClient cache;

        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;

        private readonly ConcurrentDictionary<ulong, ExperienceAdded> experienceQueue
            = new ConcurrentDictionary<ulong, ExperienceAdded>();

        private DateTime lastDbSync = DateTime.MinValue;

        private readonly ConcurrentDictionary<ulong, DateTime> lastTimeExpGranted
            = new ConcurrentDictionary<ulong, DateTime>();

        private readonly SemaphoreSlim experienceLock;
        
        private string GetContextKey(ulong guildid, ulong userid)
        {
            return $"user:{guildid}:{userid}:exp";
        }

        public AccountService(
            MikiApp app, IDiscordClient client, ISentryClient sentryClient, ICacheClient cache)
        {
            if(client == null)
            {
                throw new InvalidOperationException();
            }

            this.app = app;
            this.sentryClient = sentryClient;
            this.cache = cache;

            client.GuildMemberCreate += this.Client_UserJoinedAsync;
            client.MessageCreate += this.CheckAsync;

            experienceLock = new SemaphoreSlim(1, 1);
        }

        public async Task CheckAsync(IDiscordMessage e)
        {
            if(e.Author.IsBot)
            {
                return;
            }

            if(experienceLock.CurrentCount == 0)
            {
                return;
            }

            try
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;

                if(e is IDiscordGuildMessage guildMessage)
                {
                    var key = GetContextKey(guildMessage.GuildId, e.Author.Id);
                    if(lastTimeExpGranted
                           .GetOrAdd(e.Author.Id, DateTime.Now)
                           .AddMinutes(1) < DateTime.Now)
                    {
                        var bonusExp = MikiRandom.Next(1, 4);
                        var expObject = new ExperienceAdded
                        {
                            UserId = (long)e.Author.Id,
                            GuildId = (long)guildMessage.GuildId,
                            Experience = bonusExp,
                            Name = e.Author.Username,
                        };

                        int currentLocalExp = await cache.GetAsync<int>(key);
                        if(currentLocalExp == 0)
                        {
                            var dbContext = services.GetService<DbContext>();
                            var expProfile = await GetOrCreateExperienceProfileAsync(
                                dbContext, e.Author as IDiscordGuildUser);

                            await UpdateCacheExperienceAsync(expObject);
                            currentLocalExp = expProfile.Experience;
                        }

                        currentLocalExp += bonusExp;
                        experienceQueue.AddOrUpdate(e.Author.Id, expObject, (_, experience) =>
                        {
                            experience.Experience += expObject.Experience;
                            return experience;
                        });

                        int level = User.CalculateLevel(currentLocalExp);
                        if(User.CalculateLevel(currentLocalExp - bonusExp) != level)
                        {
                            await LevelUpLocalAsync(e, level)
                                .ConfigureAwait(false);
                        }

                        lastTimeExpGranted.AddOrUpdate(
                            e.Author.Id, DateTime.Now, (x, d) => DateTime.Now);
                        await UpdateCacheExperienceAsync(expObject);
                    }
                }

                if(DateTime.Now >= this.lastDbSync + new TimeSpan(0, 1, 0))
                {
                    try
                    {
                        await experienceLock.WaitAsync();

                        Log.Message($"Applying Experience for {this.experienceQueue.Count} users");
                        this.lastDbSync = DateTime.Now;
                        var context = services.GetService<DbContext>();

                        await UpdateGlobalDatabaseAsync(context); 
                        await UpdateLocalDatabaseAsync(context); 
                        await UpdateGuildDatabaseAsync(context);
                        await context.SaveChangesAsync();
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.Message + "\n" + ex.StackTrace);
                        sentryClient.CaptureException(ex);
                    }
                    finally
                    {
                        this.experienceQueue.Clear();
                        experienceLock.Release();
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex);
                sentryClient.CaptureException(ex);
            }
        }

        private async Task UpdateCacheExperienceAsync(ExperienceAdded experience)
        {
            var key = GetContextKey((ulong)experience.GuildId, (ulong)experience.UserId);
            await cache.UpsertAsync(key, experience.Experience, TimeSpan.FromMinutes(5));
        }

        private async Task<LocalExperience> GetOrCreateExperienceProfileAsync(
            DbContext ctx, IDiscordGuildUser user)
        {
            LocalExperience newProfile = await LocalExperience.GetAsync(ctx, user.GuildId, user.Id)
                .ConfigureAwait(false);
            if(newProfile == null)
            {
                return await LocalExperience.CreateAsync(ctx, user.GuildId, user.Id, user.Username)
                    .ConfigureAwait(false);
            }

            return newProfile;
        }

        public async Task UpdateGlobalDatabaseAsync(DbContext context)
        {
            if(this.experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, name, experience) as (values";

            List<string> userParameters = new List<string>();

            for (int i = 0; i < experienceQueue.Values.Count; i++)
            {
                userQuery.Add($"({experienceQueue.Values.ElementAt(i).UserId}, @p{i}, {experienceQueue.Values.ElementAt(i).Experience})");
                userParameters.Add(experienceQueue.Values.ElementAt(i).Name ?? "name failed to set?");
            }

            string y = "),upsert as ( update \"dbo\".\"Users\" m set \"Total_Experience\" = \"Total_Experience\" + nv.experience FROM new_values nv WHERE m.\"Id\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"Users\"(\"Id\", \"Name\", \"Total_Experience\") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up.\"Id\" = new_values.id);";

            string query = x + string.Join(",", userQuery) + y;

            await context.Database.ExecuteSqlRawAsync(query, userParameters);
        }

        public async Task UpdateLocalDatabaseAsync(DbContext context)
        {
            if(this.experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, serverid, experience) as (values ";

            for(int i = 0; i < this.experienceQueue.Values.Count; i++)
            {
                userQuery.Add(
                    $"({this.experienceQueue.Values.ElementAt(i).UserId}, {this.experienceQueue.Values.ElementAt(i).GuildId}, {this.experienceQueue.Values.ElementAt(i).Experience})");
            }

            string y =
                "),upsert as(update \"dbo\".\"LocalExperience\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"UserId\" = nv.id AND m.\"ServerId\" = nv.serverid RETURNING m.*) INSERT INTO \"dbo\".\"LocalExperience\"(\"UserId\", \"ServerId\", \"Experience\") SELECT id, serverid, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"UserId\" = new_values.id AND up.\"ServerId\" = new_values.serverid);";

            string query = x + string.Join(",", userQuery) + y;


            await context.Database.ExecuteSqlRawAsync(query);
        }

        public async Task UpdateGuildDatabaseAsync(DbContext context)
        {
            if(this.experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, experience) as (values ";

            for(int i = 0; i < this.experienceQueue.Values.Count; i++)
            {
                userQuery.Add(
                    $"({this.experienceQueue.Values.ElementAt(i).GuildId}, {this.experienceQueue.Values.ElementAt(i).Experience})");
            }

            string y =
                "),upsert as(update \"dbo\".\"GuildUsers\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"EntityId\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"GuildUsers\"(\"EntityId\", \"Experience\") SELECT id, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"EntityId\" = new_values.id);";

            string query = x + string.Join(",", userQuery) + y;

            await context.Database.ExecuteSqlRawAsync(query);
        }

        #region Events

        public async Task LevelUpLocalAsync(IDiscordMessage e, int l)
        {
            if (OnLocalLevelUp != null)
            {
                await OnLocalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
            }
        }

        public async Task LevelUpGlobalAsync(IDiscordMessage e, int l)
        {
            if (OnGlobalLevelUp != null)
            {
                await OnGlobalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
            }
        }
        
        private async Task Client_UserJoinedAsync(IDiscordGuildUser arg)
        {
            await this.UpdateGuildUserCountAsync(await arg.GetGuildAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private async Task UpdateGuildUserCountAsync(IDiscordGuild guild)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<MikiDbContext>();

            GuildUser g = await context.GuildUsers.FindAsync((long)guild.Id)
                .ConfigureAwait(false);
            g.UserCount = guild.MemberCount;

            await context.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        #endregion Events
    }

    public class ExperienceAdded
    {
        public long GuildId { get; set; }
        public long UserId { get; set; }
        public int Experience { get; set; }
        public string Name { get; set; }
    }
}