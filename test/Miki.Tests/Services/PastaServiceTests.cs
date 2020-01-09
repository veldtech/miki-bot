namespace Miki.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Services;
    using Miki.Services.Pasta.Exceptions;
    using Xunit;

    public class PastaServiceTests : BaseEntityTest<MikiDbContext>
    {
        public PastaServiceTests() 
            : base(x => new MikiDbContext(x))
        {
            var ctx = NewDbContext();
            ctx.Set<GlobalPasta>().Add(new GlobalPasta 
            {
                Id = "test",
                CreatorId = 0L,
                Score = 1,
                Text = "test body",
                TimesUsed = 1,
                CreatedAt = DateTime.Now
            });
            ctx.SaveChanges();
        }

        [Fact]
        public async Task CreatePastaTest()
        {
            var unit = NewContext();
            var service = new PastaService(unit);

            var createdPasta = await service.CreatePastaAsync("createtest", "test body", 22L);

            Assert.Equal("createtest", createdPasta.Id);
            Assert.Equal("test body", createdPasta.Text);
            Assert.Equal(22L, createdPasta.CreatorId);
            Assert.Equal(0, createdPasta.Score);
            Assert.Equal(0, createdPasta.TimesUsed);

            Assert.Equal(createdPasta, await service.GetPastaAsync(createdPasta.Id));
        }

        [Fact]
        public async Task CreateDuplicateTest()
        {
            var unit = NewContext();
            var service = new PastaService(unit);

            await Assert.ThrowsAsync<DuplicatePastaException>(
                async () => await service.CreatePastaAsync("test", "", 0L));
        }
    }
}
