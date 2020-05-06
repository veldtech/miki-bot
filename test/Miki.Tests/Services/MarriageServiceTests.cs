using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Bot.Models.Repositories;
using Miki.Services.Marriage;
using Xunit;

namespace Miki.Tests.Services
{
    public class MarriageServiceTests : BaseEntityTest<MikiDbContext>
    {
        public MarriageServiceTests()
            : base(x => new MikiDbContext(x))
        {
            var ctx = NewDbContext();
            ctx.Set<User>().Add(new User
            {
                Id = 1L, 
                DateCreated = DateTime.UtcNow,
            });
            ctx.Set<User>().Add(new User
            {
                Id = 2L,
                DateCreated = DateTime.UtcNow,
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task ProposeTestAsync()
        {
            await using(var context = NewContext())
            {
                var service = new MarriageService(context);
                await service.ProposeAsync(1L, 2L);
            }

            await using(var context = NewContext())
            {
                var service = new MarriageService(context);

                var marriage = await service.GetEntryAsync(1L, 2L);

                Assert.NotNull(marriage);
                Assert.Equal(1L, marriage.AskerId);
                Assert.Equal(2L, marriage.ReceiverId);

                Assert.NotNull(marriage.Marriage);
                Assert.Equal(marriage.MarriageId, marriage.Marriage.MarriageId);

                Assert.NotNull(marriage.Marriage.Participants);
                Assert.NotEqual(DateTime.MinValue, marriage.Marriage.TimeOfProposal);
                Assert.True(marriage.Marriage.IsProposing);
            }
        }

        [Fact]
        public async Task AcceptMarriageTest()
        {
            await InitMarriageAsync();

            await using var context = NewContext();
            var service = new MarriageService(context);
            var marriage = await service.GetMarriageAsync(1L, 2L);

            Assert.False(marriage.Marriage.IsProposing);
            Assert.NotEqual(marriage.Marriage.TimeOfProposal, marriage.Marriage.TimeOfMarriage);
        }

        [Fact]
        public async Task DeclineProposalTest()
        {
            await InitMarriageAsync();

            await using(var context = NewContext())
            {
                var service = new MarriageService(context);
                var marriage = await service.GetMarriageAsync(1L, 2L);
                await service.DeclineProposalAsync(marriage);
            }

            await using(var context = NewContext())
            {
                var service = new MarriageService(context);
                var proposal = await service.GetEntryAsync(1L, 2L);

                Assert.Null(proposal);
            }
        }

        private async Task InitMarriageAsync()
        {
            // Setup
            await using(var context = NewContext())
            {
                var service = new MarriageService(context);
                await service.ProposeAsync(1L, 2L);
            }

            // Execute
            await using(var context = NewContext())
            {
                var service = new MarriageService(context);
                var marriage = await service.GetEntryAsync(1L, 2L);

                await service.AcceptProposalAsync(marriage.Marriage);
            }
        }
    }
}
