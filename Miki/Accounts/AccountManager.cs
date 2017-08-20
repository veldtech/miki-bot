using Discord;
using IA;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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

                int randomNumber = MikiRandom.Next(0, 10);
                int currencyAdded = (l * 10 + randomNumber);

                using (var context = new MikiContext())
                {
                    User user = await context.Users.FindAsync(a.Id);

                    if (user != null)
                    {
                        await user.AddCurrencyAsync(currencyAdded, e);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        Log.Warning("User levelled up was null.");
                    }
                }

                IDiscordEmbed embed = new RuntimeEmbed(new EmbedBuilder())
                {
                    Title = locale.GetString("miki_accounts_level_up_header"),
                    Description = locale.GetString("miki_accounts_level_up_content", a.Name, l),
                    Color = new IA.SDK.Color(1, 0.7f, 0.2f)
                };

                embed.AddField(locale.GetString("miki_generic_reward"), "🔸" + currencyAdded.ToString());
                await Notification.SendChannel(e, embed);
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
                int addedExperience = MikiRandom.Next(2, 5);

                await MeruUtils.TryAsync(async () =>
                {
                    User a;
                    LocalExperience experience;

                    long userId = e.Author.Id.ToDbLong();

                    int currentGlobalLevel = 0;
                    int currentLocalLevel = 0;

                    using (var context = new MikiContext())
                    {
                        a = await context.Users.FindAsync(e.Author.Id.ToDbLong());

                        if (a == null)
                        {   
                            a = await User.CreateAsync(e);
                        }

                        experience = await context.Experience.FindAsync(e.Guild.Id.ToDbLong(), userId);

                        if (experience == null)
                        {
                            experience = await LocalExperience.CreateAsync(context, e.Guild.Id.ToDbLong(), e.Author.Id.ToDbLong());
                        }

                        if (experience.LastExperienceTime == null)
                        {
                            experience.LastExperienceTime = DateTime.Now;
                        }

                        GuildUser guildUser = await context.GuildUsers.FindAsync(e.Guild.Id.ToDbLong());
                        if (guildUser == null)
                        {
                            long guildId = e.Guild.Id.ToDbLong();
                            int? userCount = Bot.instance.Client.GetGuild(e.Guild.Id).Users.Count;                

                            int? value = await context.Experience
                                                .Where(x => x.ServerId == guildId)
                                                .SumAsync(x => x.Experience);

                            guildUser = new GuildUser();
                            guildUser.Name = e.Guild.Name;
                            guildUser.Id = guildId;
                            guildUser.Experience = value ?? 0;
                            guildUser.UserCount = userCount ?? 0;
                            guildUser.LastRivalRenewed = Utils.MinDbValue;
                            guildUser.MinimalExperienceToGetRewards = 100;

                            guildUser = context.GuildUsers.Add(guildUser);
                        }

                        currentLocalLevel = User.CalculateLevel(experience.Experience);
                        currentGlobalLevel = User.CalculateLevel(a.Total_Experience);

                        experience.Experience += addedExperience;
                        a.Total_Experience += addedExperience;
                        guildUser.Experience += addedExperience;

                        await context.SaveChangesAsync();
                    }


                    if (currentLocalLevel != User.CalculateLevel(experience.Experience))
                    {
                        await LevelUpLocalAsync(e, a, currentLocalLevel + 1);
                    }

                    if (currentGlobalLevel != User.CalculateLevel(a.Total_Experience))
                    {
                        await LevelUpGlobalAsync(e, a, currentGlobalLevel + 1);
                    }

                    lastTimeExpGranted[e.Author.Id] = DateTime.Now;
                });

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

                if (g == null)
                {
                    return;
                }

                g.UserCount = Bot.instance.Client.GetGuild(id).Users.Count;
                await context.SaveChangesAsync();
            }
        }

        #endregion Events
    }
}