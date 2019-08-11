using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Miki.Accounts.Achievements.Objects;
using Miki.Bot.Models;
using Miki.Discord.Common;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Helpers;
using Miki.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Accounts;

namespace Miki.Services.Achievements
{
	public delegate Task<bool> CheckUserUpdateAchievement(IDiscordUser ub, IDiscordUser ua);
    public delegate Task<bool> CheckCommandAchievement(User u, Node e);

	public class AchievementService
	{
        private readonly Dictionary<string, AchievementDataContainer> _containers
            = new Dictionary<string, AchievementDataContainer>();

		public event Func<AchievementPacket, Task> OnAchievementUnlocked;

		public event Func<CommandPacket, Task> OnCommandUsed;

		public event Func<LevelPacket, Task> OnLevelGained;

		public event Func<MessageEventPacket, Task> OnMessage;

		public event Func<TransactionPacket, Task> OnTransaction;

		public AchievementService(AccountService service)
		{
            service.OnLocalLevelUp += async (u, c, l) =>
			{
				LevelPacket p = new LevelPacket()
				{
					discordUser = await (c as IDiscordGuildChannel).GetUserAsync(u.Id),
					discordChannel = c,
					level = l,
				};
				await OnLevelGained?.Invoke(p);
			};

            service.OnTransactionMade += async (msg, u1, u2, amount) =>
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
			};
		}

        public AchievementDataContainer GetContainerById(string id)
		{
			if(this._containers.ContainsKey(id))
			{
				return this._containers[id];
			}

			Log.Warning($"Could not load AchievementContainer {id}");
			return null;
		}

		public string PrintAchievements(List<Achievement> achievementNames)
		{
            if(achievementNames == null 
               || !achievementNames.Any())
            {
                return string.Empty;
            }

            string output = string.Empty;
            foreach(var a in achievementNames)
            {
                if(!this._containers.TryGetValue(a.Name, out var value))
                {
                    continue;
                }

                if(a.Rank < value.Achievements.Count)
                {
                    output += value.Achievements[a.Rank].Icon + " ";
                }
            }
			return output;
		}

        public async Task CallAchievementUnlockEventAsync(
            DbContext context,
            IAchievement achievement, 
            IDiscordUser user,
            IDiscordTextChannel channel)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if(!(achievement is AchievementAchievement))
            {
                // Ignore event if this achievement was gained from this event.
                return;
            }

            long id = (long)user.Id;

            int achievementCount = await context
                .Set<Achievement>()
                .Where(q => q.UserId == id)
                .CountAsync()
                .ConfigureAwait(false);

            AchievementPacket p = new AchievementPacket()
            {
                discordUser = user,
                discordChannel = channel,
                achievement = achievement,
                count = achievementCount
            };

            if(this.OnAchievementUnlocked != null)
            {
                await this.OnAchievementUnlocked.Invoke(p)
                    .ConfigureAwait(false);
            }
        }

        public async Task CallTransactionMadeEventAsync(IDiscordGuildChannel m, User receiver, User giver, int amount)
		{
			try
			{
				TransactionPacket p = new TransactionPacket();
				if(m is IDiscordTextChannel tc)
				{
					p.discordChannel = tc;
				}
				p.discordUser = await m.GetUserAsync(receiver.Id.FromDbLong());

				if(giver != null)
				{
					p.giver = giver;
				}

				p.receiver = receiver;

				p.amount = amount;

				if(OnTransaction != null)
				{
					await OnTransaction?.Invoke(p);
				}
			}
			catch(Exception e)
			{
				Log.Warning($"Achievement check failed: {e.ToString()}");
			}
		}

        public void AddAchievement(string name, params IAchievement[] achievements)
        {
            if(!achievements.Any())
            {
                throw new ArgumentNullException(nameof(achievements));
            }

            var a = new AchievementDataContainer(this)
            {
                Name = name
            };

            a.Achievements.AddRange(achievements);
            this._containers.Add(name, a);
        }
    }
}