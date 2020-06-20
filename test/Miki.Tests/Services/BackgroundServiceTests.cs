using System.Threading.Tasks;
using Miki.API.Backgrounds;
using Miki.Bot.Models;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services;
using Miki.Services.Transactions;
using Moq;
using Xunit;

namespace Miki.Tests.Services
{
    public class BackgroundServiceTests
    {
        private readonly Mock<IUnitOfWork> unitOfWorkMock;
        private readonly Mock<ITransactionService> transactionServiceMock;
        private readonly Mock<IAsyncRepository<BackgroundsOwned>> backgroundsOwnedRepositoryMock;
        private readonly Mock<IAsyncRepository<ProfileVisuals>> profileVisualsRepositoryMock;
        private readonly Mock<IBackgroundStore> backgroundStoreMock;
        private readonly IBackgroundService backgroundService;
        
        public BackgroundServiceTests()
        {
            unitOfWorkMock = new Mock<IUnitOfWork>();
            backgroundsOwnedRepositoryMock = new Mock<IAsyncRepository<BackgroundsOwned>>();
            profileVisualsRepositoryMock = new Mock<IAsyncRepository<ProfileVisuals>>();

            unitOfWorkMock.Setup(x => x.GetRepository(
                It.IsAny<IRepositoryFactory<BackgroundsOwned>>()))
                .Returns(backgroundsOwnedRepositoryMock.Object);
            unitOfWorkMock.Setup(x => x.GetRepository(
                It.IsAny<IRepositoryFactory<ProfileVisuals>>()))
                .Returns(profileVisualsRepositoryMock.Object);

            transactionServiceMock = new Mock<ITransactionService>();
            backgroundStoreMock = new Mock<IBackgroundStore>();

            backgroundService = new BackgroundService(
                unitOfWorkMock.Object, transactionServiceMock.Object, backgroundStoreMock.Object);
        }
        
        [Fact]
        public async Task SetBackgroundAddAsync()
        {
            backgroundsOwnedRepositoryMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(new ValueTask<BackgroundsOwned>(new BackgroundsOwned()));
            await backgroundService.SetBackgroundAsync(1L, 1);

            profileVisualsRepositoryMock.Verify(
                x => x.AddAsync(It.Is<ProfileVisuals>(x => x.BackgroundId == 1 && x.UserId == 1L)));
            unitOfWorkMock.Verify(x => x.CommitAsync());
        }

        [Fact]
        public async Task SetBackgroundEditAsync()
        {
            backgroundsOwnedRepositoryMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(new ValueTask<BackgroundsOwned>(new BackgroundsOwned()));
            profileVisualsRepositoryMock.Setup(x => x.GetAsync(It.IsAny<long>()))
                .Returns(new ValueTask<ProfileVisuals>(new ProfileVisuals { UserId = 1L }));

            await backgroundService.SetBackgroundAsync(1L, 1);

            profileVisualsRepositoryMock.Verify(
                x => x.EditAsync(It.Is<ProfileVisuals>(x => x.BackgroundId == 1 && x.UserId == 1L)));
            unitOfWorkMock.Verify(x => x.CommitAsync());
        }

        [Fact]
        public async Task PurchaseBackgroundAsync()
        {
            backgroundStoreMock.Setup(x => x.GetBackgroundAsync(It.IsAny<int>()))
                .Returns(new ValueTask<Background>(new Background { Id = 1, Price = 100 }));
            backgroundsOwnedRepositoryMock.Setup(x => x.GetAsync(It.IsAny<long>(), It.IsAny<int>()))
                .Returns(new ValueTask<BackgroundsOwned>());

            await backgroundService.PurchaseBackgroundAsync(1L, 1);

            transactionServiceMock.Verify(
                x => x.CreateTransactionAsync(It.IsAny<TransactionRequest>()));
            backgroundsOwnedRepositoryMock.Verify(
                x => x.AddAsync(It.Is<BackgroundsOwned>(x => x.BackgroundId == 1 && x.UserId == 1L)));
            unitOfWorkMock.Verify(x => x.CommitAsync());
        }

        [Fact]
        public async Task PurchaseBackgroundOwnedAsync()
        {
            backgroundsOwnedRepositoryMock.Setup(x => x.GetAsync(It.IsAny<object[]>()))
                .Returns(() => new ValueTask<BackgroundsOwned>(new BackgroundsOwned()));

            await Assert.ThrowsAsync<BackgroundOwnedException>(
                async () => await backgroundService.PurchaseBackgroundAsync(1L, 1));
        }
    }
}