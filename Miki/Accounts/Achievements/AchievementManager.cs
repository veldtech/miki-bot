using Microsoft.EntityFrameworkCore;
using Miki.Accounts.Achievements.Objects;
using Miki.Cache;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Events;
using Miki.Helpers;
using Miki.Logging;
using Miki.Models;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Accounts.Achievements
{
	public delegate Task<bool> CheckUserUpdateAchievement(IDiscordUser ub, IDiscordUser ua);

	public delegate Task<bool> CheckCommandAchievement(User u, CommandEvent e);

	public class AchievementManager
	{
		private static AchievementManager _instance;

		public static AchievementManager Instance
		{
			get
			{
				if (_instance == null)
					_instance = new AchievementManager(MikiApp.Instance);

				return _instance;
			}
		}

		internal BaseService provider = null;

		private readonly MikiApp bot;
		private readonly Dictionary<string, AchievementDataContainer> containers = new Dictionary<string, AchievementDataContainer>();

		public event Func<AchievementPacket, Task> OnAchievementUnlocked;

		public event Func<CommandPacket, Task> OnCommandUsed;

		public event Func<LevelPacket, Task> OnLevelGained;

		public event Func<MessageEventPacket, Task> OnMessage;

		public event Func<TransactionPacket, Task> OnTransaction;

        public AchievementManager(MikiApp bot)
		{
			this.bot = bot;

			AccountManager.Instance.OnLocalLevelUp += async (u, c, l) =>
			{
                using (var db = new MikiContext())
                {
                    if (await provider.IsEnabled(MikiApp.Instance.GetService<ICacheClient>(), db, c.Id))
                    {
                        LevelPacket p = new LevelPacket()
                        {
                            discordUser = await (c as IDiscordGuildChannel).GetUserAsync(u.Id),
                            discordChannel = c,
                            level = l,
                        };
                        await OnLevelGained?.Invoke(p);
                    }
                }
			};

			AccountManager.Instance.OnTransactionMade += async (msg, u1, u2, amount) =>
			{
                using (var db = new MikiContext())
                {
                    if (await provider.IsEnabled(MikiApp.Instance.GetService<ICacheClient>(), db, msg.ChannelId))
                    {
                        TransactionPacket p = new TransactionPacket()
                        {
                            discordUser = msg.Author,
                            discordChannel = await msg.GetChannelAsync(),
                            giver = u1,
                            receiver = u2,
                            amount = amount
                        };

                        await OnTransaction?.Invoke(p);
                    }
                }
			};

			bot.GetService<EventSystem>().GetCommandHandler<SimpleCommandHandler>().OnMessageProcessed += async (e, m, t) =>
			{
				CommandPacket p = new CommandPacket()
				{
					discordUser = m.Author,
					discordChannel = await m.GetChannelAsync(),
					message = m,
					command = e,
					success = true
				};
				await OnCommandUsed?.Invoke(p);
			};
		}

		internal void AddContainer(AchievementDataContainer container)
		{
			if (containers.ContainsKey(container.Name))
			{
				Log.Warning($"AddContainer cannot add duplicate containers");
				return;
			}

			containers.Add(container.Name, container);
		}

		public AchievementDataContainer GetContainerById(string id)
		{
			if (containers.ContainsKey(id))
			{
				return containers[id];
			}

			Log.Warning($"Could not load AchievementContainer {id}");
			return null;
		}

		public string PrintAchievements(List<Achievement> achievementNames)
		{
			string output = "";
			foreach (var a in achievementNames)
			{
				if (containers.TryGetValue(a.Name, out var value))
				{
					if (a.Rank < value.Achievements.Count)
					{
						output += value.Achievements[a.Rank].Icon + " ";
					}
				}
			}
			return output;
		}

		public async Task CallAchievementUnlockEventAsync(IAchievement achievement, IDiscordUser user, IDiscordTextChannel channel)
		{
			DogStatsd.Counter("achievements.gained", 1);

            if (achievement as AchievementAchievement != null)
            {
                return;
            }

			long id = user.Id.ToDbLong();

			using (var context = new MikiContext())
			{
				int achievementCount = await context.Achievements
					.Where(q => q.UserId == id)
					.CountAsync();

				AchievementPacket p = new AchievementPacket()
				{
					discordUser = user,
					discordChannel = channel,
					achievement = achievement,
					count = achievementCount
				};

				await OnAchievementUnlocked?.Invoke(p);
			}
		}

		public async Task CallTransactionMadeEventAsync(IDiscordGuildChannel m, User receiver, User giver, int amount)
		{
			try
			{
				TransactionPacket p = new TransactionPacket();
                if (m is IDiscordTextChannel tc)
                {
                    p.discordChannel = tc;
                }
				p.discordUser = await m.GetUserAsync(receiver.Id.FromDbLong());

				if (giver != null)
				{
					p.giver = giver;
				}

				p.receiver = receiver;

				p.amount = amount;

				if (OnTransaction != null)
				{
					await OnTransaction?.Invoke(p);
				}
			}
			catch (Exception e)
			{
				Log.Warning($"Achievement check failed: {e.ToString()}");
			}
		}

        /// <summary>
        /// Unlocks the achievement and if not yet added to the database, It'll add it to the database.
        /// </summary>
        /// <param name="context">sql context</param>
        /// <param name="id">user id</param>
        /// <param name="r">rank set to (optional)</param>
        /// <returns></returns>
        public async Task UnlockAsync(IAchievement achievement, IDiscordTextChannel channel, IDiscordUser user, int r = 0)
        {
            long userid = user.Id.ToDbLong();

            if (await UnlockIsValid(achievement, userid, r))
            {
                await CallAchievementUnlockEventAsync(achievement, user, channel);            
                await Notification.SendAchievementAsync(achievement, channel, user);
            }
        }

        public async Task UnlockAsync(IAchievement achievement, IDiscordUser user, int r = 0)
        {
            long userid = user.Id.ToDbLong();

            if (await UnlockIsValid(achievement, userid, r))
            {
                await Notification.SendAchievementAsync(achievement, user);
            }
        }

        public async Task<bool> UnlockIsValid(IAchievement achievement, long userId, int newRank)
        {
            using (var context = new MikiContext())
            {
                var achievementObject = await DatabaseHelpers.GetAchievementAsync(context, userId, achievement.ParentName);

                // If no achievement has been found and want to unlock first
                if (achievementObject == null && newRank == 0)
                {
                    achievementObject = context.Achievements.Add(new Achievement()
                    {
                        UserId = userId,
                        Name = achievement.ParentName,
                        Rank = 0
                    }).Entity;

                    await DatabaseHelpers.UpdateCacheAchievementAsync(userId, achievement.Name, achievementObject);
                    await context.SaveChangesAsync();
                    return true;
                }
                // If achievement we want to unlock is the next achievement
                if (achievementObject != null)
                {
                    if (achievementObject.Rank == newRank - 1)
                    {
                        achievementObject.Rank++;
                    }
                    else
                    {
                        return false;
                    }

                    await DatabaseHelpers.UpdateCacheAchievementAsync(userId, achievement.ParentName, achievementObject);
                    await context.SaveChangesAsync();
                    return true;
                }
            }
            return false;
        }
    }
}