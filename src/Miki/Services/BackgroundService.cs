using Miki.API.Backgrounds;
using Miki.Bot.Models;
using Miki.Exceptions;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services.Transactions;
using System.Linq;
using System.Threading.Tasks;
using Miki.Functional;

namespace Miki.Services
{
    public interface IBackgroundService
    {
        /// <summary>
        /// Gets the user's set background.
        /// </summary>
        ValueTask<Background> GetBackgroundAsync(long userId);

        /// <summary>
        /// Purchases the background with id <paramref name="backgroundId"/> for the user.
        /// </summary>
        ValueTask PurchaseBackgroundAsync(long userId, int backgroundId);

        /// <summary>
        /// Sets the user's preferred background to <paramref name="backgroundId"/>
        /// </summary>
        ValueTask SetBackgroundAsync(long userId, int backgroundId);
    }

    public class BackgroundService : IBackgroundService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly ITransactionService transactionService;
        private readonly IBackgroundStore backgroundStore;
        private readonly IAsyncRepository<BackgroundsOwned> backgroundRepository;
        private readonly IAsyncRepository<ProfileVisuals> profileVisualsRepository;

        public BackgroundService(
            IUnitOfWork unitOfWork, 
            ITransactionService transactionService, 
            IBackgroundStore backgroundStore)
        {
            this.unitOfWork = unitOfWork;
            this.transactionService = transactionService;
            this.backgroundStore = backgroundStore;

            backgroundRepository = unitOfWork.GetRepository<BackgroundsOwned>();
            profileVisualsRepository = unitOfWork.GetRepository<ProfileVisuals>();
        }

        public async ValueTask SetBackgroundAsync(long userId, int backgroundId)
        {
            var background = await backgroundRepository.GetAsync(userId, backgroundId);
            if (background == null)
            {
                throw new BackgroundNotOwnedException();
            }

            var profileVisuals = await profileVisualsRepository.GetAsync(userId);
            if(profileVisuals == null)
            {
                await profileVisualsRepository.AddAsync(
                    new ProfileVisuals
                    {
                        UserId = userId,
                        BackgroundId = backgroundId
                    });
            }
            else
            {
                profileVisuals.BackgroundId = backgroundId;
                await profileVisualsRepository.EditAsync(profileVisuals);
            }
            await unitOfWork.CommitAsync();
        }

        public async ValueTask PurchaseBackgroundAsync(long userId, int backgroundId)
        {
            var background = await backgroundRepository.GetAsync(userId, backgroundId);
            if(background != null)
            {
                throw new BackgroundOwnedException();
            }

            var backgroundResource = await backgroundStore.GetBackgroundAsync(backgroundId);

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                .WithReceiver(AppProps.Currency.BankId)
                .WithSender(userId)
                .WithAmount(backgroundResource.Price)
                .Build());

            await backgroundRepository.AddAsync(new BackgroundsOwned
            {
                UserId = userId,
                BackgroundId = backgroundId
            });
            
            await unitOfWork.CommitAsync();
        }

        public async ValueTask<Background> GetBackgroundAsync(long userId)
        {
            var backgroundSet = await backgroundRepository.GetAsync(userId);
            var backgroundId = backgroundSet?.BackgroundId ?? 0;
            return await backgroundStore.GetBackgroundAsync(backgroundId);
        }
    }
}
