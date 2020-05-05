namespace Miki.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Miki.Bot.Models;

    public interface IUserService : IDisposable
    {
        ValueTask AddMarriageSlotAsync(long userId);

        ValueTask<User> CreateUserAsync(long userId, string userName);

        ValueTask<User> GetUserAsync(long userId);

        /// <summary>
        /// Gets the current amount of reputation the user can spend.
        /// </summary>
        ValueTask<ReputationObject> GetUserReputationPointsAsync(long userId);

        /// <summary>
        /// Give reputation to other users.
        /// </summary>
        /// <param name="senderId">The user sending reputation.</param>
        /// <param name="receivers">The user receiving a set amount.</param>
        ValueTask<GiveReputationResponse> GiveReputationAsync(
            long senderId, IReadOnlyList<GiveReputationRequest> receivers);

        [Obsolete("Wrap update calls inside of a service function instead.")]
        ValueTask UpdateUserAsync(User user);

        ValueTask<bool> UserIsDonatorAsync(long userId);

        ValueTask<bool> UserIsBannedAsync(long userId);

        [Obsolete("Wrap save calls inside of a service function instead.")]
        ValueTask SaveAsync();
    }
}