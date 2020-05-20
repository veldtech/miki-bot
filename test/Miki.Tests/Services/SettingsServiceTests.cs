using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Modules;
using Miki.Services.Settings;
using Xunit;

namespace Miki.Tests.Services
{
    public class SettingsServiceTests : BaseEntityTest<MikiDbContext>
    {
        private static readonly long defaultEntityId = 1L;

        public SettingsServiceTests()
            : base(x => new MikiDbContext(x))
        {
            using var context = NewDbContext();
            context.Set<Setting>().Add(new Setting
            {
                EntityId = defaultEntityId,
                SettingId = (int)SettingType.Achievements,
                Value = (int)AchievementNotificationSetting.None
            });
            context.SaveChanges();
        }



        [Fact]
        public async Task GetSettingThrowsNullTest()
        {
            await using var context = NewContext();
            var service = new SettingsService(context);

            var value = await service.GetAsync<AchievementNotificationSetting>(
                SettingType.Achievements, 345893745L);

            Assert.Throws<EntityNullException<Setting>>(() => value.Unwrap());

        }

        [Fact]
        public async Task GetSettingTest()
        {
            await using var context = NewContext();
            var service = new SettingsService(context);

            var value = await service.GetAsync<AchievementNotificationSetting>(
                SettingType.Achievements, defaultEntityId);
            Assert.Equal(AchievementNotificationSetting.None, value.Unwrap());
        }

        [Fact]
        public async Task SetSettingTest()
        {
            await using(var context = NewContext())
            {
                var service = new SettingsService(context);
                await service.SetAsync(
                    SettingType.Achievements, 2L, AchievementNotificationSetting.None);
            }

            await using(var context = NewContext())
            {
                var repo = context.GetRepository<Setting>();
                var value = await repo.GetAsync(2L, 1);

                Assert.Equal(1, value.Value);
            }
        }
    }
}
