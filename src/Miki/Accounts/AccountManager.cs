namespace Miki.Accounts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Cache;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Localization;
    using Miki.Localization;
    using Miki.Logging;
    using Miki.Modules;

    public delegate Task LevelUpDelegate(IDiscordUser a, IDiscordTextChannel g, int level);

    public class AccountService
    {
        public event LevelUpDelegate OnLocalLevelUp;

        public event LevelUpDelegate OnGlobalLevelUp;

        public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;

        private readonly ConcurrentDictionary<ulong, ExperienceAdded> _experienceQueue
            = new ConcurrentDictionary<ulong, ExperienceAdded>();

        private DateTime _lastDbSync = DateTime.MinValue;

        private readonly ConcurrentDictionary<ulong, DateTime> _lastTimeExpGranted
            = new ConcurrentDictionary<ulong, DateTime>();

        private bool _isSyncing;

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

            this.OnLocalLevelUp += async (a, e, l) =>
            {
                IDiscordGuild guild = await ((IDiscordGuildChannel)e).GetGuildAsync()
                    .ConfigureAwait(false);
                long guildId = guild.Id.ToDbLong();

                using(var scope = MikiApp.Instance.Services.CreateScope())
                {
                    MikiDbContext context = scope.ServiceProvider.GetService<MikiDbContext>();
                    List<LevelRole> rolesObtained = await context.LevelRoles
                        .Where(p => p.GuildId == guildId && p.RequiredLevel == l && p.Automatic)
                        .ToListAsync()
                        .ConfigureAwait(false);

                    LevelNotificationsSetting setting = (LevelNotificationsSetting)await Setting
                        .GetAsync(context, e.Id, DatabaseSettingId.LevelUps)
                        .ConfigureAwait(false);

                    switch(setting)
                    {
                        case LevelNotificationsSetting.NONE:
                        case LevelNotificationsSetting.RewardsOnly when rolesObtained.Count == 0:
                            return;
                        case LevelNotificationsSetting.All:
                            break;

                        default:
                            throw new InvalidOperationException();
                    }

                    LocalizationPipelineStage pipeline = scope.ServiceProvider
                        .GetService<LocalizationPipelineStage>();

                    IResourceManager instance = await pipeline
                        .GetLocaleAsync(
                            scope.ServiceProvider,
                            (long)e.Id)
                        .ConfigureAwait(false);

                    EmbedBuilder embed = new EmbedBuilder
                    {
                        Title = instance.GetString("miki_accounts_level_up_header"),
                        Description = instance.GetString(
                            "miki_accounts_level_up_content",
                            $"{a.Username}#{a.Discriminator}",
                            l),
                        Color = new Color(1, 0.7f, 0.2f)
                    };

                    if(rolesObtained.Count > 0)
                    {
                        List<IDiscordRole> roles = (await guild.GetRolesAsync().ConfigureAwait(false))
                            .ToList();

                        IDiscordGuildUser guildUser = await guild.GetMemberAsync(a.Id)
                            .ConfigureAwait(false);
                        if(guildUser != null)
                        {
                            foreach(LevelRole role in rolesObtained)
                            {
                                IDiscordRole r = roles.FirstOrDefault(x => x.Id == (ulong)role.RoleId);
                                if(r == null)
                                {
                                    continue;
                                }

                                await guildUser.AddRoleAsync(r)
                                    .ConfigureAwait(false);
                            }
                        }

                        embed.AddInlineField("Rewards",
                            string.Join(
                                "\n",
                                rolesObtained
                                    .Select(x =>
                                        $"New Role: **{roles.FirstOrDefault(z => z.Id.ToDbLong() == x.RoleId)?.Name}**")));
                    }

                    await embed.ToEmbed()
                        .QueueAsync(e)
                        .ConfigureAwait(false);
                }
            };

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

            if(this._isSyncing)
            {
                return;
            }

            try
            {
                if(await e.GetChannelAsync()
                    .ConfigureAwait(false) is IDiscordGuildChannel channel)
                {
                    ICacheClient cache = MikiApp.Instance.Services.GetService<ICacheClient>();

                    string key = this.GetContextKey(channel.GuildId, e.Author.Id);

                    if(this._lastTimeExpGranted.GetOrAdd(e.Author.Id, DateTime.Now).AddMinutes(1) < DateTime.Now)
                    {
                        int currentLocalExp;
                        if(!await cache.ExistsAsync(key).ConfigureAwait(false))
                        {
                            using(var scope = MikiApp.Instance.Services.CreateScope())
                            {
                                DbContext db = scope.ServiceProvider.GetService<DbContext>();
                                LocalExperience user = await LocalExperience.GetAsync(
                                        db,
                                        channel.GuildId,
                                        e.Author.Id)
                                    .ConfigureAwait(false);
                                if(user == null)
                                {
                                    user = await LocalExperience.CreateAsync(
                                            db,
                                            channel.GuildId,
                                            e.Author.Id,
                                            e.Author.Username)
                                        .ConfigureAwait(false);
                                }

                                await cache.UpsertAsync(key, user.Experience)
                                    .ConfigureAwait(false);
                                currentLocalExp = user.Experience;
                            }
                        }
                        else
                        {
                            currentLocalExp = await cache.GetAsync<int>(key);
                        }

                        var bonusExp = MikiRandom.Next(1, 4);
                        currentLocalExp += bonusExp;

                        if (!_experienceQueue.ContainsKey(e.Author.Id))
                        {
                            var expObject = new ExperienceAdded()
                            {
                                UserId = e.Author.Id.ToDbLong(),
                                GuildId = channel.GuildId.ToDbLong(),
                                Experience = bonusExp,
                                Name = e.Author.Username,
                            };

                            this._experienceQueue.AddOrUpdate(e.Author.Id, expObject, (u, eo) =>
                            {
                                eo.Experience += expObject.Experience;
                                return eo;
                            });
                        }
                        else
                        {
                            this._experienceQueue[e.Author.Id].Experience += bonusExp;
                        }

                        int level = User.CalculateLevel(currentLocalExp);

                        if(User.CalculateLevel(currentLocalExp - bonusExp) != level)
                        {
                            await this.LevelUpLocalAsync(e, level)
                                .ConfigureAwait(false);
                        }

                        this._lastTimeExpGranted.AddOrUpdate(e.Author.Id, DateTime.Now, (x, d) => DateTime.Now);

                        await cache.UpsertAsync(key, currentLocalExp)
                            .ConfigureAwait(false);
                    }
                }

                if(DateTime.Now >= this._lastDbSync + new TimeSpan(0, 1, 0))
                {
                    this._isSyncing = true;
                    Log.Message($"Applying Experience for {this._experienceQueue.Count} users");
                    this._lastDbSync = DateTime.Now;

                    try
                    {
                        using(var scope = MikiApp.Instance.Services.CreateScope())
                        {

                            var context = scope.ServiceProvider.GetService<DbContext>();

                            await this.UpdateGlobalDatabaseAsync(context)
                                .ConfigureAwait(false);
                            await this.UpdateLocalDatabaseAsync(context)
                                .ConfigureAwait(false);
                            await this.UpdateGuildDatabaseAsync(context)
                                .ConfigureAwait(false);

                            await context.SaveChangesAsync()
                                .ConfigureAwait(false);
                        }
                    }
                    catch(Exception ex)
                    {
                        Log.Error(ex.Message + "\n" + ex.StackTrace);
                    }
                    finally
                    {
                        this._experienceQueue.Clear();
                        this._isSyncing = false;
                    }

                    Log.Message("Done Applying!");
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public async Task UpdateGlobalDatabaseAsync(DbContext context)
        {
            if(this._experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, name, experience) as (values";

            List<string> userParameters = new List<string>();

            for (int i = 0; i < _experienceQueue.Values.Count; i++)
            {
                userQuery.Add($"({_experienceQueue.Values.ElementAt(i).UserId}, @p{i}, {_experienceQueue.Values.ElementAt(i).Experience})");
                userParameters.Add(_experienceQueue.Values.ElementAt(i).Name ?? "name failed to set?");
            }

            string y = $"),upsert as ( update \"dbo\".\"Users\" m set \"Total_Experience\" = \"Total_Experience\" + nv.experience FROM new_values nv WHERE m.\"Id\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"Users\"(\"Id\", \"Name\", \"Total_Experience\") SELECT id, name, experience FROM new_values WHERE NOT EXISTS(SELECT * FROM upsert up WHERE up.\"Id\" = new_values.id);";

            string query = x + string.Join(",", userQuery) + y;

            await context.Database.ExecuteSqlCommandAsync(query, userParameters.ToArray());
        }

        public async Task UpdateLocalDatabaseAsync(DbContext context)
        {
            if(this._experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, serverid, experience) as (values ";

            for(int i = 0; i < this._experienceQueue.Values.Count; i++)
            {
                userQuery.Add(
                    $"({this._experienceQueue.Values.ElementAt(i).UserId}, {this._experienceQueue.Values.ElementAt(i).GuildId}, {this._experienceQueue.Values.ElementAt(i).Experience})");
            }

            string y =
                "),upsert as(update \"dbo\".\"LocalExperience\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"UserId\" = nv.id AND m.\"ServerId\" = nv.serverid RETURNING m.*) INSERT INTO \"dbo\".\"LocalExperience\"(\"UserId\", \"ServerId\", \"Experience\") SELECT id, serverid, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"UserId\" = new_values.id AND up.\"ServerId\" = new_values.serverid);";

            string query = x + string.Join(",", userQuery) + y;


            await context.Database.ExecuteSqlCommandAsync(query);
        }

        public async Task UpdateGuildDatabaseAsync(DbContext context)
        {
            if(this._experienceQueue.Count == 0)
            {
                return;
            }

            List<string> userQuery = new List<string>();
            string x = "WITH new_values (id, experience) as (values ";

            for(int i = 0; i < this._experienceQueue.Values.Count; i++)
            {
                userQuery.Add(
                    $"({this._experienceQueue.Values.ElementAt(i).GuildId}, {this._experienceQueue.Values.ElementAt(i).Experience})");
            }

            string y =
                "),upsert as(update \"dbo\".\"GuildUsers\" m set \"Experience\" = \"Experience\" + nv.experience FROM new_values nv WHERE m.\"EntityId\" = nv.id RETURNING m.*) INSERT INTO \"dbo\".\"GuildUsers\"(\"EntityId\", \"Experience\") SELECT id, experience FROM new_values WHERE NOT EXISTS(SELECT 1 FROM upsert up WHERE up.\"EntityId\" = new_values.id);";

            string query = x + string.Join(",", userQuery) + y;

            await context.Database.ExecuteSqlCommandAsync(query);
        }

        #region Events

        public async Task LevelUpLocalAsync(IDiscordMessage e, int l)
        {
            await this.OnLocalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
        }

        public async Task LevelUpGlobalAsync(IDiscordMessage e, int l)
        {
            await this.OnGlobalLevelUp.Invoke(e.Author, await e.GetChannelAsync(), l);
        }

        public Task LogTransactionAsync(IDiscordMessage msg, User receiver, User fromUser, int amount)
        {
            return this.OnTransactionMade.Invoke(msg, receiver, fromUser, amount);
        }

        private async Task Client_GuildUpdated(IDiscordGuild arg1, IDiscordGuild arg2)
        {
            if(arg1.Name != arg2.Name)
            {
                using(var scope = MikiApp.Instance.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetService<MikiDbContext>();

                    GuildUser g = await context.GuildUsers.FindAsync(arg1.Id.ToDbLong())
                        .ConfigureAwait(false);
                    g.Name = arg2.Name;

                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task Client_UserJoined(IDiscordGuildUser arg)
        {
            await this.UpdateGuildUserCountAsync(await arg.GetGuildAsync().ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private async Task UpdateGuildUserCountAsync(IDiscordGuild guild)
        {
            using(var scope = MikiApp.Instance.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<MikiDbContext>();

                GuildUser g = await context.GuildUsers.FindAsync(guild.Id.ToDbLong())
                    .ConfigureAwait(false);
                g.UserCount = guild.MemberCount;

                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
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