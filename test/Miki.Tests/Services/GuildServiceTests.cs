using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Services;
using Miki.Services.Transactions;
using Xunit;

namespace Miki.Tests.Services
{
    public class GuildServiceTests : BaseEntityTest<MikiDbContext>
    {
        /// <inheritdoc />
        public GuildServiceTests()
            : base(x => new MikiDbContext(x))
        {
            using var ctx = NewDbContext();
            ctx.Set<GuildUser>().Add(new GuildUser
            {
                Id = 1,
                MinimalExperienceToGetRewards = 100,
                Experience = 5000,
                RivalId = 2
            });
            ctx.Set<GuildUser>().Add(new GuildUser
            {
                Id = 2,
                MinimalExperienceToGetRewards = 100,
                Experience = 4000,
                RivalId = 1
            });
            ctx.Set<GuildUser>().Add(new GuildUser
            {
                Id = 3,
                MinimalExperienceToGetRewards = 100,
                Experience = 4000
            });

            ctx.Set<User>().Add(new User
            {
                Id = 1,
                Currency = 1000000
            });
            ctx.Set<User>().Add(new User
            {
                Id = 2,
                Currency = 0
            });
            ctx.Set<User>().Add(new User
            {
                Id = 3,
                Currency = 0
            });
            ctx.Set<User>().Add(new User
            {
                Id = 4,
                Currency = 0
            });

            ctx.Set<LocalExperience>().Add(new LocalExperience
            {
                ServerId = 1,
                UserId = 2,
                Experience = 200
            });
            ctx.Set<LocalExperience>().Add(new LocalExperience
            {
                ServerId = 1,
                UserId = 3,
                Experience = 200
            });
            ctx.Set<LocalExperience>().Add(new LocalExperience
            {
                ServerId = 1,
                UserId = 4,
                Experience = 50
            });
            ctx.Set<LocalExperience>().Add(new LocalExperience
            {
                ServerId = 2,
                UserId = 2,
                Experience = 200
            });

            ctx.Set<Timer>().Add(new Timer
            {
                GuildId = 1,
                UserId = 2,
                Value = DateTime.UtcNow.AddDays(-30)
            });
            ctx.Set<Timer>().Add(new Timer
            {
                GuildId = 1,
                UserId = 3,
                Value = DateTime.UtcNow
            });
            ctx.Set<Timer>().Add(new Timer
            {
                GuildId = 1,
                UserId = 4,
                Value = DateTime.UtcNow.AddDays(-30)
            });
            ctx.Set<Timer>().Add(new Timer
            {
                GuildId = 2,
                UserId = 2,
                Value = DateTime.UtcNow.AddDays(-30)
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task ClaimWeeklySuccessTest()
        {
            var response = await ClaimWeeklyTest(new GuildUserReference(1L, 2L));

            Assert.Equal(WeeklyStatus.Success, response.Status);
            Assert.NotEqual(0, response.AmountClaimed);
        }

        [Fact]
        public async Task ClaimWeeklyNotReadyTest()
        {
            var response = await ClaimWeeklyTest(new GuildUserReference(1L, 3L));

            Assert.Equal(WeeklyStatus.NotReady, response.Status);
            Assert.InRange(DateTime.UtcNow, response.LastClaimTime.AddSeconds(-1), response.LastClaimTime.AddSeconds(1));
        }

        [Fact]
        public async Task ClaimWeeklyUserInsufficientExpTest()
        {
            var response = await ClaimWeeklyTest(new GuildUserReference(1L, 4L));

            Assert.Equal(WeeklyStatus.UserInsufficientExp, response.Status);
        }

        [Fact]
        public async Task ClaimWeeklyGuildInsufficientExpTest()
        {
            var response = await ClaimWeeklyTest(new GuildUserReference(2L, 2L));

            Assert.Equal(WeeklyStatus.GuildInsufficientExp, response.Status);
        }

        [Fact]
        public async Task ClaimWeeklyNoRivalTest()
        {
            await Assert.ThrowsAsync<EntityNullException<GuildUser>>(async () =>
            {
                await ClaimWeeklyTest(new GuildUserReference(3L, 2L));
            });
        }

        public async Task<WeeklyResponse> ClaimWeeklyTest(GuildUserReference guildUserReference)
        {
            await using var unit = NewContext();
            var userService = new UserService(unit, null);
            var transactionService = new TransactionService(userService);
            var guildService = new GuildService(unit, transactionService);

            return await guildService.ClaimWeeklyAsync(guildUserReference);
        }
    }
}
