namespace Miki.Accounts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Framework.Extension;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Localization;
    using Miki.Localization.Models;
    using Miki.Logging;
    using Miki.Modules;

    public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordTextChannel g, int level);

    public class AccountService
    {
        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;

        private readonly ConcurrentDictionary<ulong, ExperienceAdded> experienceQueue
            = new ConcurrentDictionary<ulong, ExperienceAdded>();

        private DateTime lastDbSync = DateTime.MinValue;

        private readonly ConcurrentDictionary<ulong, DateTime> lastTimeExpGranted
            = new ConcurrentDictionary<ulong, DateTime>();

        private bool isSyncing;

        private string GetContextKey(ulong guildid, ulong userid)
        {
            return $"user:{guildid}:{userid}:exp";
        }

        public AccountService(IDiscordClient client)
        {
            if(client == null)
            {
                throw new InvalidOperationException();
            }

            //discord.guildUpdate += Client_GuildUpdated;
            client.GuildMemberCreate += this.Client_UserJoined;
            client.MessageCreate += this.CheckAsync;
        }

        public async Task CheckAsync(IDiscordMessage e)
        {
            if(e.Author.IsBot)
            {
                return;
            }

            if(this.isSyncing)
            {
                return;
            }

            try
            {
                var channel = await e.GetChannelAsync().ConfigureAwait(false);
                if(channel is IDiscordGuildChannel guildChannel)
                {
                    ICacheClient cache = MikiApp.Instance.Services.GetService<ICacheClient>();

                    string key = this.GetContextKey(guildChannel.GuildId, e.Author.Id);

                    if(this.lastTimeExpGranted
                           .GetOrAdd(e.Author.Id, DateTime.Now)
                           .AddMinutes(1) < DateTime.Now)
                    {
                        int currentLocalExp = await cache.GetAsync<int>(key);
                        if(currentLocalExp == 0)
                        {
                            // TODO: remove MikiApp.Instance
                            using var scope = MikiApp.Instance.Services.CreateScope();
                            DbContext db = scope.ServiceProvider.GetService<DbContext>();
                            
                            var expProfile = await GetOrCreateExperienceProfile(
                                db, e.Author as IDiscordGuildUser);

                            await cache.UpsertAsync(key, expProfile.Experience)
                                .ConfigureAwait(false);
                            currentLocalExp = expProfile.Experience;
                        }

                        var bonusExp = MikiRandom.Next(1, 4);
                        currentLocalExp += bonusExp;

                        var expObject = new ExperienceAdded()
                        {
                            UserId = e.Author.Id.ToDbLong(),
                            GuildId = guildChannel.GuildId.ToDbLong(),
                            Experience = bonusExp,
                            Name = e.Author.Username,
                        };

                        this.experienceQueue.AddOrUpdate(e.Author.Id, expObject, (u, eo) =>
                        {
                            eo.Experience += expObject.Experience;
                            return eo;
                        });

                        int level = User.CalculateLevel(currentLocalExp);

                        if(User.CalculateLevel(currentLocalExp - bonusExp) != level)
                        {
                            await this.LevelUpLocalAsync(e, level)
                                .ConfigureAwait(false);
                        }

                        this.lastTimeExpGranted.AddOrUpdate(
                            e.Author.Id, DateTime.Now, (x, d) => DateTime.Now);

                        await cache.UpsertAsync(key, currentLocalExp)
                            .ConfigureAwait(false);
                    }
                }

                if(DateTime.Now >= this.lastDbSync + new TimeSpan(0, 1, 0))
                {
                    this.isSyncing = true;
                    Log.Message($"Applying Experience for {this.experienceQueue.Count} users");
                    this.lastDbSync = DateTime.Now;

                    try
                    {
                        using var scope = MikiApp.Instance.Services.CreateScope();
                        var context = scope.ServiceProvider.GetService<DbContext>();

                        var _ = Task.WhenAll(
                                UpdateGlobalDatabaseAsync(context),
                                UpdateLocalDatabaseAsync(context),
                                UpdateGuildDatabaseAsync(context))
                            .ContinueWith(x => context.SaveChangesAsync());
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.Message + "\n" + ex.StackTrace);
                    }
                    finally
                    {
                        this.experienceQueue.Clear();
                        this.isSyncing = false;
                    }

                    Log.Message("Done Applying!");
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private async Task<LocalExperience> GetOrCreateExperienceProfile(
            DbContext ctx, IDiscordGuildUser user)
        {
            LocalExperience newProfile = await LocalExperience.GetAsync(
                    ctx, user.GuildId, user.Id)
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

            string y = $"),upsert as ( update \"dbo\".\"Users\" m set \"Total_Experience\" = \"Total_Experience\" + nv.experience FROM new_values nv WHERE m.\"Id\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"Users\"(\"Id\", \"Name\", \"Total_Experience\") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up.\"Id\" = new_values.id);";

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

        private async Task Client_GuildUpdated(IDiscordGuild before, IDiscordGuild after)
        {
            if(before.Name != after.Name)
            {
                using var scope = MikiApp.Instance.Services.CreateScope();
                var context = scope.ServiceProvider.GetService<MikiDbContext>();

                GuildUser g = await context.GuildUsers.FindAsync(before.Id.ToDbLong())
                    .ConfigureAwait(false);
                g.Name = after.Name;

                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        private async Task Client_UserJoined(IDiscordGuildUser arg)
        {
            await this.UpdateGuildUserCountAsync(await arg.GetGuildAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private async Task UpdateGuildUserCountAsync(IDiscordGuild guild)
        {
            using var scope = MikiApp.Instance.Services.CreateScope();
            var context = scope.ServiceProvider.GetService<MikiDbContext>();

            GuildUser g = await context.GuildUsers.FindAsync(guild.Id.ToDbLong())
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