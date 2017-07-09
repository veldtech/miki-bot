using IA;
using System.Collections.Generic;
using System.Threading.Tasks;
using IA.SDK.Interfaces;
using System;
using Discord;
using IA.SDK;
using Miki.Models;
using System.Linq;
using Miki.Accounts.Achievements.Objects;
using System.Data.SqlClient;

namespace Miki.Accounts.Achievements
{
    public delegate Task<bool> CheckUserUpdateAchievement(IDiscordUser ub, IDiscordUser ua);
    public delegate Task<bool> CheckCommandAchievement(User u, CommandEvent e);

    public class AchievementManager
    {
        private static AchievementManager _instance = new AchievementManager(Bot.instance);
        public static AchievementManager Instance => _instance;

        private Bot bot;
        private Dictionary<string, AchievementDataContainer<BaseAchievement>> containers = new Dictionary<string, AchievementDataContainer<BaseAchievement>>();

        public event Func<AchievementPacket, Task> OnAchievementUnlocked;
        public event Func<CommandPacket, Task> OnCommandUsed;
        public event Func<LevelPacket, Task> OnLevelGained;
        public event Func<MessageEventPacket, Task> OnMessageReceived;
        public event Func<TransactionPacket, Task> OnTransaction;

        // Veld - WARNING: does not work with channel messages 
        public event Func<UserUpdatePacket, Task> OnUserUpdate;

        private AchievementManager(Bot bot)
        {
            this.bot = bot;

            AccountManager.Instance.OnGlobalLevelUp += async (u, c, l) =>
            {
                LevelPacket p = new LevelPacket()
                {
                    discordUser = await c.Guild.GetUserAsync(u.Id.FromDbLong()),
                    discordChannel = c,
                    account = u,
                    level = l,
                };
                await OnLevelGained?.Invoke(p);
            };
            AccountManager.Instance.OnTransactionMade += async (msg, u1, u2, amount) =>
            {
                TransactionPacket p = new TransactionPacket()
                {
                    discordUser = msg.Author,
                    discordChannel = msg.Channel,
                    giver = u1,
                    receiver = u2,
                    amount = amount
                };

                await OnTransaction?.Invoke(p);
            };

            bot.Client.MessageReceived += async (e) =>
            {
                if(OnMessageReceived == null)
                {
                    return;
                }

                MessageEventPacket p = new MessageEventPacket()
                {
                    message = new RuntimeMessage(e),
                    discordUser = new RuntimeUser(e.Author),
                    discordChannel = new RuntimeMessageChannel(e.Channel)                    
                };
                await OnMessageReceived?.Invoke(p);
            };
            bot.Client.UserUpdated += async (ub, ua) =>
            {
                UserUpdatePacket p = new UserUpdatePacket()
                {
                    discordUser = new RuntimeUser(ub),
                    userNew = new RuntimeUser(ua)
                };
                await OnUserUpdate?.Invoke(p);
            };
            bot.Events.AddCommandDoneEvent(x =>
            {
                x.Name = "--achievement-manager-command";
                x.processEvent = async (m, e, s) =>
                {
                    CommandPacket p = new CommandPacket()
                    {
                        discordUser = m.Author,
                        discordChannel = m.Channel,
                        message = m,
                        command = e,
                        success = s
                    };
                    await OnCommandUsed?.Invoke(p);
                };
            });

        }

        internal void AddContainer(AchievementDataContainer<BaseAchievement> container)
        {
            if (containers.ContainsKey(container.Name))
            {
                Log.WarningAt("AddContainer", "Cannot add duplicate containers");
                return;
            }

            containers.Add(container.Name, container);
        }

        public AchievementDataContainer<BaseAchievement> GetContainerById(string id)
        {
            if (containers.ContainsKey(id))
            {
                return containers[id];
            }

            Log.Warning($"Could not load AchievementContainer {id}");
            return null;
        }

        public string PrintAchievements(MikiContext context, ulong userid)
        {
            string output = "";
            long id = userid.ToDbLong();

            List<Achievement> achievements = context.Achievements.Where(p => p.Id == id).ToList();
            
            foreach (Achievement achievement in achievements)
            {
                output += containers[achievement.Name].Achievements[achievement.Rank].Icon + " ";
            }
            return output;
        }

        public async Task CallAchievementUnlockEventAsync(BaseAchievement achievement, IDiscordUser user)
        {
            if (achievement as AchievementAchievement != null) return;

            using (var context = new MikiContext())
            {
                long id = user.Id.ToDbLong();

                List<Achievement> achs = context.Achievements.Where(q => q.Id == id).ToList();
                int achievementCount = achs.Sum(x => x.Rank+1);

                AchievementPacket p = new AchievementPacket()
                {
                    discordUser = user,
                    achievement = achievement,
                    count = achievementCount
                };

                await OnAchievementUnlocked?.Invoke(p);
            }
        }
    }
}