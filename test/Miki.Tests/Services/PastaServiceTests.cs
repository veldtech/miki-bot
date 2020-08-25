using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Framework.Commands.Scopes;
using Miki.Services;
using Miki.Services.Pasta;
using Miki.Services.Pasta.Exceptions;
using Moq;
using Xunit;

namespace Miki.Tests.Services
{
    public class PastaServiceTests : BaseEntityTest<MikiDbContext>
    {
        private readonly GlobalPasta testPasta = new GlobalPasta
        {
            Id = "test",
            CreatorId = 0L,
            Score = 1,
            Text = "test body",
            TimesUsed = 1,
            CreatedAt = DateTime.Now
        };

        private readonly Mock<IScopeService> scopeServiceMock;

        public PastaServiceTests()
            : base(x => new MikiDbContext(x))
        {
            var ctx = NewDbContext();
            ctx.Set<GlobalPasta>().Add(testPasta);
            ctx.Set<PastaVote>().Add(new PastaVote
            {
                Id = "test",
                PositiveVote = true,
                UserId = 124L
            });
            ctx.SaveChanges();

            scopeServiceMock = new Mock<IScopeService>();
        }

        [Fact]
        public async Task CreatePastaTestAsync()
        {
            var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);

            var createdPasta = await service.CreatePastaAsync("createtest", "test body", 22L);
            Assert.Equal(createdPasta, await service.GetPastaAsync(createdPasta.Id));
        }

        [Fact]
        public async Task CreateDuplicateTestAsync()
        {
            var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);

            await Assert.ThrowsAsync<DuplicatePastaException>(
                async () => await service.CreatePastaAsync(testPasta.Id, "", 0L));
        }

        [Fact]
        public async Task GetPastaTestAsync()
        {
            var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);

            var pasta = await service.GetPastaAsync(testPasta.Id);

            Assert.Equal(testPasta.Id, pasta.Id);
            Assert.Equal(testPasta.Text, pasta.Text);
            Assert.Equal(testPasta.Score, pasta.Score);
            Assert.Equal(testPasta.CreatorId, pasta.CreatorId);
            Assert.Equal(testPasta.CreatedAt, pasta.CreatedAt);
            Assert.Equal(testPasta.TimesUsed, pasta.TimesUsed);
        }

        [Fact]
        public async Task DeletePastaTestAsync()
        {
            scopeServiceMock.Setup(x => x.HasScopeAsync(It.IsAny<long>(), It.IsAny<IEnumerable<string>>()))
                .Returns(() => new ValueTask<bool>(false));

            using(var unit = NewContext())
            {
                var service = new PastaService(unit, scopeServiceMock.Object);

                await service.DeletePastaAsync(testPasta.Id, 0L);
                await unit.CommitAsync();
            }

            using(var unit = NewContext())
            {
                var service = new PastaService(unit, scopeServiceMock.Object);

                await Assert.ThrowsAsync<PastaNotFoundException>(
                    async () => await service.GetPastaAsync(testPasta.Id));
            }
        }

        [Fact]
        public async Task DeleteUnauthorizedPastaTestAsync()
        {
            scopeServiceMock.Setup(x => x.HasScopeAsync(It.IsAny<long>(), It.IsAny<IEnumerable<string>>()))
                .Returns(() => new ValueTask<bool>(false));

            var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);

            await Assert.ThrowsAsync<ActionUnauthorizedException>(
                async () => await service.DeletePastaAsync(testPasta.Id, 1L));
        }

        [Fact]
        public async Task SearchPastaTestAsync()
        {
            var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);

            var result = await service.SearchPastaAsync(x => true, 12, 0);
            Assert.Equal(1, result.PageIndex);
            Assert.Equal(1, result.PageCount);

            Assert.Equal(1, result.Items.Count);
        }

        [Fact]
        public async Task VotePastaTestAsync()
        {
            using(var unit = NewContext())
            {
                var service = new PastaService(unit, scopeServiceMock.Object);

                await service.VoteAsync(new PastaVote
                {
                    Id = "test",
                    PositiveVote = true,
                    UserId = 0L
                });
                await unit.CommitAsync();
            }

            using(var unit = NewContext())
            {
                var service = new PastaService(unit, scopeServiceMock.Object);
                var vote = await service.GetVoteAsync("test", 0L);

                Assert.NotNull(vote);
                Assert.True(vote.PositiveVote);
            }
        }

        [Fact]
        public async Task GetVotesTestAsync()
        {
            using var unit = NewContext();
            var service = new PastaService(unit, scopeServiceMock.Object);
            var count = await service.GetVotesAsync("test");

            Assert.Equal(
                new VoteCount(1, 0),
                count);
        }
    }
}
