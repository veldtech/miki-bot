namespace Miki.Services.Achievements
{
    using Miki.Bot.Models;
    using Miki.Discord.Common;
    using Miki.Framework.Commands;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Framework;
    using Patterns.Repositories;

    public delegate Task<bool> CheckUserUpdateAchievement(
        IDiscordUser userBefore, IDiscordUser userAfter);
    public delegate Task<bool> CheckCommandAchievement(User user, Node command);

	public class AchievementService
	{
        private readonly Dictionary<string, AchievementObject> containers
            = new Dictionary<string, AchievementObject>();

        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<Achievement> repository;

		public event Func<IContext, AchievementEntry, Task> OnAchievementUnlocked;

		public AchievementService(IUnitOfWork unitOfWork, IRepositoryFactory<Achievement> factory)
        {
            this.unitOfWork = unitOfWork;
            repository = unitOfWork.GetRepository(factory);
        }
        
        public void AddAchievement(AchievementObject @object)
        {
            if (containers.ContainsKey(@object.Id))
            {
                throw new ArgumentException(
                    $"Achievement with name '{@object.Id}' already exists.");
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

        public Task UnlockAsync(IContext context, string achievementName, ulong userId, int rank = 0)
        {
            var achievement = GetAchievement(achievementName);
            return UnlockAsync(context, achievement, userId, rank);  
        }
        public async Task UnlockAsync(IContext context, AchievementObject achievement, ulong userId, int rank = 0)
        {
            if (rank >= achievement.Entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            var currentAchievement = await repository.GetAsync((long)userId, achievement.Id);
            if ((currentAchievement?.Rank ?? -1) >= rank)
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

            await unitOfWork.CommitAsync();

            if (OnAchievementUnlocked != null)
            {
                await OnAchievementUnlocked(context, achievement.Entries[rank]);
            }
        }
    }
}