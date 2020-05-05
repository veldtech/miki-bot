namespace Miki.Tests.Services
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Framework;
    using Miki.Modules.Accounts.Services;
    using Miki.Services.Achievements;
    using Moq;
    using Xunit;

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
                var service = NewService(unit);
                await service.UnlockAsync(new TestContextObject(), "test", 0);
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
            var service = NewService();

            service.OnAchievementUnlocked.Subscribe(x =>
            {
                hasFired = true;
            });

            await service.UnlockAsync(new TestContextObject(), "test", 0);
            Assert.True(hasFired);
        }

        private AchievementService NewService(IUnitOfWork work = null)
        {
            var collection = new AchievementCollection();
            collection.AddAchievement(new AchievementObject.Builder("test").Add("x").Build());
            return new AchievementService(work ?? NewContext(), collection, null);
        }
    }   
}
