using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Framework;
using Miki.Modules.Accounts.Services;
using Miki.Services.Achievements;
using Moq;
using Xunit;
using Miki.Bot.Models.Repositories;

namespace Miki.Tests.Services
{
    public class AchievementServiceTests : BaseEntityTest<MikiDbContext>
    {
        public AchievementServiceTests()
            : base(x => new MikiDbContext(x))
        {}

        [Fact(DisplayName = "Unlocked achievements should save to database")]
        public async Task AchievementUnlockShouldSaveToDatabaseAsync()
        {
            await using(var unit = NewContext())
            {
                var service = NewService(unit).Item1;
                await service.UnlockAsync("test", 0);
            }

            await using(var unit = NewContext())
            {
                var repository = unit.GetRepository<Achievement>();
                var achievement = await repository.GetAsync(0L, "test", (short)0);

                Assert.NotNull(achievement);
                Assert.NotEqual(achievement.UnlockedAt, DateTime.MinValue);
            }
        }

        [Fact(DisplayName = "Unlocked achievements should call OnAchievementUnlocked event")]
        public async Task AchievementUnlockShouldFireEventAsync()
        {
            var hasFired = false;
            var (service, events) = NewService();
            bool ranCallback = false;
            events.OnAchievementUnlocked.Subscribe(x =>
            {
                hasFired = true;
                ranCallback = true;
            });

            await service.UnlockAsync("test", 0);
            while (!ranCallback) { }
            Assert.True(hasFired);
        }

        private (AchievementService, AchievementEvents) NewService(IUnitOfWork work = null)
        {
            var collection = new AchievementCollection();
            collection.AddAchievement(new AchievementObject.Builder("test").Add("x").Build());
            var events = new AchievementEvents();
            return (new AchievementService(
                work ?? NewContext(), 
                collection,
                new AchievementRepository.Factory(),
                events), events);
        }
    }   
}
