using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using Miki.Bot.Models.Repositories;

namespace Miki.Services.Achievements
{
    using Patterns.Repositories;

    public delegate Task<bool> CheckUserUpdateAchievement(IDiscordUser ub, IDiscordUser ua);
    public delegate Task<bool> CheckCommandAchievement(User u, Node e);

	public class AchievementService
	{
        private readonly Dictionary<string, AchievementObject> containers
            = new Dictionary<string, AchievementObject>();

        private readonly IAsyncRepository<Achievement> repository;

		public event Func<AchievementObject, Task> OnAchievementUnlocked;

		public AchievementService(IAsyncRepository<Achievement> achievements)
		{
            repository = achievements;
        }
        
        public void AddAchievement(AchievementObject @object)
        {
            if (containers.ContainsKey(@object.Id))
            {
                throw new ArgumentException(
                    "Achievement with name " + @object.Id + " already exists.");
            }
            containers.Add(@object.Id, @object);
        }

        public AchievementObject GetAchievementOrDefault(string id)
            => containers.TryGetValue(id, out var achievement) 
                ? achievement 
                : null;

        public AchievementObject GetAchievement(string id) 
            => GetAchievementOrDefault(id) 
               ?? throw new InvalidOperationException("Achievement not found");

        public string PrintAchievements(List<Achievement> achievements)
		{
            if(achievements == null || !achievements.Any())
            {
                return string.Empty;
            }

            string output = string.Empty;
            foreach(var a in achievements)
            {
                if(!this.containers.TryGetValue(a.Name, out var value))
                {
                    continue;
                }

                if(a.Rank < value.Entries.Count)
                {
                    output += value.Entries.ElementAt(a.Rank).Icon + " ";
                }
            }
			return output;
		}

        public async Task UnlockAsync(
            DbContext context, AchievementObject achievement, ulong userId, int rank = 0)
        {
            if (achievement.Entries.Count >= rank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            var currentAchievement = await repository.GetAsync(achievement.Id, (long)userId);
            if (currentAchievement.Rank >= rank)
            {
                return;
            }

            await repository.AddAsync(new Achievement
            {
                Name = achievement.Id,
                Rank = (short) rank,
                UnlockedAt = DateTime.UtcNow,
                UserId = (long) userId
            });

            await context.SaveChangesAsync();

            if (OnAchievementUnlocked != null)
            {
                await OnAchievementUnlocked(achievement);
            }
        }
    }
}