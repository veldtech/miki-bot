using Miki.Bot.Models;
using Miki.Discord.Common;
using Miki.Framework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Miki.Framework;
using Microsoft.EntityFrameworkCore;
using Miki.Patterns.Repositories;
using Miki.Modules.Accounts.Services;
using System.Reactive.Subjects;

namespace Miki.Services.Achievements
{
    public delegate Task<bool> CheckUserUpdateAchievement(
        IDiscordUser userBefore, IDiscordUser userAfter);
    public delegate Task<bool> CheckCommandAchievement(User user, Node command);

	public class AchievementService
	{
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<Achievement> repository;
        private readonly AchievementCollection achievementCollection;

        private readonly Subject<(IContext, AchievementEntry)> achievementUnlockedSubject;

        public IObservable<(IContext, AchievementEntry)> OnAchievementUnlocked
            => achievementUnlockedSubject;

		public AchievementService(
            IUnitOfWork unitOfWork, 
            AchievementCollection achievementCollection,
            IRepositoryFactory<Achievement> factory)
        {
            this.achievementCollection = achievementCollection;
            this.unitOfWork = unitOfWork;
            repository = unitOfWork.GetRepository(factory);

            achievementUnlockedSubject = new Subject<(IContext, AchievementEntry)>();
        }
        
        public AchievementObject GetAchievementOrDefault(string id)
            => achievementCollection.TryGetAchievement(id, out var achievement) 
                ? achievement 
                : null;

        public AchievementObject GetAchievement(string id) 
            => GetAchievementOrDefault(id) 
               ?? throw new InvalidOperationException("Achievement not found");

        public async Task<IEnumerable<Achievement>> GetUnlockedAchievementsAsync(long userId)
        {
            return await (await repository.ListAsync())
                .AsQueryable()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }

        public string PrintAchievements(List<Achievement> achievements)
		{
            if(achievements == null || !achievements.Any())
            {
                return string.Empty;
            }

            string output = string.Empty;
            foreach(var a in achievements)
            {
                if(!achievementCollection.TryGetAchievement(a.Name, out var value))
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

        public Task UnlockAsync(
            IContext context, string achievementName, ulong userId, int rank = 0)
        {
            var achievement = GetAchievement(achievementName);
            return UnlockAsync(context, achievement, userId, rank);  
        }
        public async Task UnlockAsync(
            IContext context, AchievementObject achievement, ulong userId, int rank = 0)
        {
            if (rank >= achievement.Entries.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            var entry = achievement.Entries[rank];
            var currentAchievement = await repository.GetAsync(
                (long)userId, achievement.Id);
            if ((currentAchievement?.Rank ?? -1) >= rank)
            {
                return;
            }

            await repository.AddAsync(entry.ToModel(userId));

            await unitOfWork.CommitAsync();
            // TODO: separate for daemon?
            if (!(context.GetChannel() is null))
            {
                achievementUnlockedSubject.OnNext((context, entry));
             }
        }
    }
}