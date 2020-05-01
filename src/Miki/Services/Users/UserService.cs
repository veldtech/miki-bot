namespace Miki.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Bot.Models;
    using Bot.Models.Exceptions;
    using Bot.Models.Models.User;
    using Framework;
    using Miki.Cache;
    using Patterns.Repositories;

    public class UserService : IUserService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly ICacheClient cache;
        private readonly IAsyncRepository<User> repository;
        private readonly IAsyncRepository<IsBanned> bannedRepository;
        private readonly IAsyncRepository<IsDonator> donatorRepository;

        public UserService(IUnitOfWork unitOfWork, ICacheClient cache)
        {
            this.unitOfWork = unitOfWork;
            this.cache = cache;
            this.repository = unitOfWork.GetRepository<User>();
            this.bannedRepository = unitOfWork.GetRepository<IsBanned>();
            this.donatorRepository = unitOfWork.GetRepository<IsDonator>();
        }

        /// <inheritdoc />
        public async ValueTask<User> CreateUserAsync(long userId, string userName)
        {
            var user = new User
            {
                Id = userId,
                DateCreated = DateTime.UtcNow,
                Name = userName,
                MarriageSlots = 1,
            };
            await repository.AddAsync(user);
            await unitOfWork.CommitAsync();
            return user;
        }

        /// <inheritdoc />
        public async ValueTask<User> GetUserAsync(long userId)
        {
            var user = await repository.GetAsync(userId);
            if (user == null)
            {
                throw new EntityNullException<User>();
            }
            return user;
        }

        /// <inheritdoc />
        public async ValueTask<ReputationObject> GetUserReputationPointsAsync(long userId)
        {
            var repObject = await cache.GetAsync<ReputationObject>($"user:{userId}:rep");
            if(repObject != null)
            {
                return repObject;
            }

            repObject = new ReputationObject
            {
                LastReputationGiven = DateTime.Now,
                ReputationPointsLeft = 3
            };

            await cache.UpsertAsync(
                $"user:{userId}:rep",
                repObject,
                DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow
            );
            return repObject;
        }

        /// <inheritdoc />
        public async ValueTask<GiveReputationResponse> GiveReputationAsync(
            long senderId,
            IReadOnlyList<GiveReputationRequest> receivers)
        {
            if(!receivers.Any())
            {
                throw new ReputationGiveEmptyException();
            }

            var repObject = await GetUserReputationPointsAsync(senderId);
            var totalGiven = receivers.Sum(x => x.Amount);
            if(totalGiven > repObject.ReputationPointsLeft)
            {
                throw new ReputationLimitOverflowException(
                    receivers.Count, totalGiven, repObject.ReputationPointsLeft);
            }

            var usersReceived = new List<User>();
            foreach(var receiver in receivers)
            {
                var user = await GetUserAsync(receiver.RecieverId);
                if(user == null)
                {
                    continue;
                }

                if(receiver.Amount <= 0)
                {
                    continue;
                }

                user.Reputation += receiver.Amount;
                await repository.EditAsync(user);
                usersReceived.Add(user);
            }

            if(!usersReceived.Any())
            {
                throw new ReputationGiveEmptyException();
            }

            await unitOfWork.CommitAsync();

            repObject.ReputationPointsLeft -= (short)receivers
                .Where(x => usersReceived.FirstOrDefault(user => user.Id == x.RecieverId) != null)
                .Sum(x => x.Amount);

            await cache.UpsertAsync(
                $"user:{senderId}:rep", repObject, DateTime.UtcNow.AddDays(1).Date - DateTime.UtcNow);

            return new GiveReputationResponse
            {
                UsersReceived = usersReceived,
                ReputationLeft = repObject.ReputationPointsLeft
            };
        }

        /// <inheritdoc />
        public async ValueTask<bool> UserIsDonatorAsync(long userId)
        {
            var donatorStatus = await donatorRepository.GetAsync(userId);
            if(donatorStatus == null)
            {
                return false;
            }

            return donatorStatus.ValidUntil > DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async ValueTask<bool> UserIsBannedAsync(long userId)
        {
            var banRecord = await bannedRepository.GetAsync(userId);
            if (banRecord == null)
            {
                return false;
            }
            return banRecord.ExpirationDate > DateTime.UtcNow;
        }

        /// <inheritdoc />
        public ValueTask UpdateUserAsync(User user)
            => repository.EditAsync(user);

        /// <inheritdoc />
        public ValueTask SaveAsync()
            => unitOfWork.CommitAsync();

        /// <inheritdoc />
        public void Dispose()
            => unitOfWork?.Dispose();
    }

    public class GiveReputationResponse
    {
        public IReadOnlyList<User> UsersReceived { get; set; }
        public int ReputationLeft { get; set; }
    }
}
