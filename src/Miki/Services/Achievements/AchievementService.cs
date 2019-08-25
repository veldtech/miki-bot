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
	public delegate Task<bool> CheckUserUpdateAchievement(IDiscordUser ub, IDiscordUser ua);
    public delegate Task<bool> CheckCommandAchievement(User u, Node e);

	public class AchievementService
	{
        private readonly Dictionary<string, AchievementObject> _containers
            = new Dictionary<string, AchievementObject>();

        private readonly AchievementRepository _repository;

		public event Func<AchievementObject, Task> OnAchievementUnlocked;

		public AchievementService(
            AccountService service,
            AchievementRepository achievements)
		{
            _repository = achievements;
        }
        
        public void AddAchievement(AchievementObject @object)
        {
            if (_containers.ContainsKey(@object.Id))
            {
                throw new ArgumentException(
                    "Achievement with name " + @object.Id + " already exists.");
            }
            _containers.Add(@object.Id, @object);
        }

        public AchievementObject GetAchievementOrDefault(string id)
            => _containers.TryGetValue(id, out var achievement) 
                ? achievement 
                : null;

        public AchievementObject GetAchievement(string id) 
            => GetAchievementOrDefault(id) 
               ?? throw new InvalidOperationException("Achievement not found");

        public string PrintAchievements(List<Achievement> achievements)
		{
            if(achievements == null 
               || !achievements.Any())
            {
                return string.Empty;
            }

            string output = string.Empty;
            foreach(var a in achievements)
            {
                if(!this._containers.TryGetValue(a.Name, out var value))
                {
                    continue;
                }

                if(a.Rank < value.Entries.Count())
                {
                    output += value.Entries.ElementAt(a.Rank).Icon + " ";
                }
            }
			return output;
		}

        public async Task UnlockAsync(DbContext context, AchievementObject achievement, ulong userId, int rank = 0)
        {
            if (achievement.Entries.Count >= rank)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            var currentAchievement = await _repository.GetAsync(achievement.Id, (long)userId);
            if (currentAchievement.Rank >= rank)
            {
                return;
            }

            await _repository.AddAsync(new Achievement
            {
                Name = achievement.Id,
                Rank = (short) rank,
                UnlockedAt = DateTime.UtcNow,
                UserId = (long) userId
            });

            await context.SaveChangesAsync();
        }
    }
}