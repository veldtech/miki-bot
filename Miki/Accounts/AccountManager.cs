using Discord;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miki.Accounts
{
    public delegate Task LevelUpDelegate(User a, IDiscordMessageChannel g, int level);

    public class AccountManager
    {
        private static AccountManager _instance = new AccountManager(Bot.instance);
        public static AccountManager Instance => _instance;

        public event LevelUpDelegate OnLocalLevelUp;
        public event LevelUpDelegate OnGlobalLevelUp;
        public event Func<IDiscordMessage, User, User, int, Task> OnTransactionMade;

        private Bot bot;

        private Dictionary<ulong, DateTime> lastTimeExpGranted = new Dictionary<ulong, DateTime>();

        private AccountManager(Bot bot)
        {
            this.bot = bot;

            OnLocalLevelUp += async (a, e, l) =>
            {
                Locale locale = Locale.GetEntity(e.Id.ToDbLong());

                int randomNumber = Global.random.Next(0, 10);
                int currencyAdded = (l * 10 + randomNumber);

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder());
                embed.Title = locale.GetString("miki_accounts_level_up_header");
                embed.Description = locale.GetString("miki_accounts_level_up_content", a.Name, l);
                embed.AddField(x => { x.Name = locale.GetString("miki_generic_reward"); x.Value = "🔸" + currencyAdded.ToString(); });
                embed.Color = new IA.SDK.Color(1, 0.7f, 0.2f);
                await Notification.SendChannel(e, embed);

                using (var context = new MikiContext())
                {
                    User user = await context.Users.FindAsync(a.Id);
                    user.AddCurrency(context, null, currencyAdded);
                    await context.SaveChangesAsync();
                }
            };

            Bot.instance.Client.GuildUpdated += Client_GuildUpdated;
            Bot.instance.Client.UserJoined += Client_UserJoined;
            Bot.instance.Client.UserLeft += Client_UserLeft;
        }

        public async Task CheckAsync(IDiscordMessage e)
        {
            if (e.Author.IsBot) return;

            if (!lastTimeExpGranted.ContainsKey(e.Author.Id))
            {
                lastTimeExpGranted.Add(e.Author.Id, DateTime.MinValue);
            }

            if (lastTimeExpGranted[e.Author.Id].AddMinutes(1) < DateTime.Now)
            {
                using (var context = new MikiContext())
                {
                    User a = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                    if (a == null)
                    {
                        a = User.Create(context, e);
                    }

                    try
                    {
                        LocalExperience experience = await context.Experience.FindAsync(e.Guild.Id.ToDbLong(), a.Id);

                        if (experience == null)
                        {
                            experience = context.Experience.Add(new LocalExperience() { server_id = e.Guild.Id.ToDbLong(), user_id = a.Id, Experience = 0, LastExperienceTime = DateTime.Now - new TimeSpan(1) });
                        }

                        GuildUser guildUser = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());

                        if (guildUser == null)
                        {
                            int value = await context.Database.SqlQuery<int>
                                ("select Sum(Experience) as value from LocalExperience where ServerId = @p0;", e.Guild.Id.ToDbLong())
                                .FirstAsync();

                            guildUser = context.GuildUsers.Add(new GuildUser()
                            {
                                Name = e.Guild.Name,
                                Id = e.Guild.Id.ToDbLong(),
                                Experience = value,
                                UserCount = Bot.instance.Client.GetGuild(e.Guild.Id).Users.Count,
                                LastRivalRenewed = DateTime.Now.AddYears(-1),
                                LastRewardClaimed = DateTime.Now.AddYears(-1)
                            });
                            await context.SaveChangesAsync();
                        }

                        if (experience.LastExperienceTime == null)
                        {
                            experience.LastExperienceTime = DateTime.Now;
                        }

                        int currentLocalLevel = a.CalculateLevel(experience.Experience);
                        int currentGlobalLevel = a.CalculateLevel(a.Total_Experience);
                        int addedExperience = Global.random.Next(1, 10);

                        experience.Experience += addedExperience;
                        a.Total_Experience += addedExperience;
                        guildUser.Experience += addedExperience;

                        if (currentLocalLevel != a.CalculateLevel(experience.Experience))
                        {
                            await LevelUpLocalAsync(e, a, currentLocalLevel + 1);
                        }
                        if (currentGlobalLevel != a.CalculateLevel(a.Total_Experience))
                        {
                            await LevelUpGlobalAsync(e, a, currentGlobalLevel + 1);
                        }

                        experience.LastExperienceTime = DateTime.Now;
                        lastTimeExpGranted[e.Author.Id] = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorAt("Accounts.Check", ex.Message + ex.StackTrace + ex.Source);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }

        #region Events
        public async Task LevelUpLocalAsync(IDiscordMessage e, User a, int l)
        {
            await OnLocalLevelUp.Invoke(a, e.Channel, l);
        }
        public async Task LevelUpGlobalAsync(IDiscordMessage e, User a, int l)
        {
            await OnGlobalLevelUp.Invoke(a, e.Channel, l);
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

        private async Task Client_UserLeft(Discord.WebSocket.SocketGuildUser arg)
        {
            await UpdateGuildUserCountAsync(arg.Guild.Id);
        }
        private async Task Client_UserJoined(Discord.WebSocket.SocketGuildUser arg)
        {
            await UpdateGuildUserCountAsync(arg.Guild.Id);
        }

        private async Task UpdateGuildUserCountAsync(ulong id)
        {
            using (MikiContext context = new MikiContext())
            {
                GuildUser g = await context.GuildUsers.FindAsync(id.ToDbLong());

                if(g == null)
                {
                    return;
                }

                g.UserCount = Bot.instance.Client.GetGuild(id).Users.Count;
                await context.SaveChangesAsync();
            }
        }
        #endregion
    }
}