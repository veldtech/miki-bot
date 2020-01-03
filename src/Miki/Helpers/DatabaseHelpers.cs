namespace Miki.Helpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Cache;
    using Miki.Discord.Common;
    using Miki.Framework;
    using StatsdClient;

    public static class DatabaseHelpers
	{
        public static async Task<Achievement> GetAchievementAsync(
            IContext context, long userId, string name)
        {
            string key = $"achievement:{userId}:{name}";

            var cache = context.GetService<ICacheClient>();
            var database = context.GetService<DbContext>();

            Achievement a = await cache.GetAsync<Achievement>(key);
            if (a != null)
            {
                return database.Attach(a).Entity;
            }

            Achievement achievement = await database.Set<Achievement>()
                .FindAsync(userId, name);
            if (achievement != null)
            {
                await cache.UpsertAsync(key, achievement);
            }

            return achievement;
        }

		internal static async Task UpdateCacheAchievementAsync(
            long userId, string name, Achievement achievement)
		{
            var cache = MikiApp.Instance.Services.GetService<ICacheClient>();
            string key = $"achievement:{userId}:{name}";
			await cache.UpsertAsync(key, achievement);
		}

		public static void AddCurrency(
            this User user, 
            int amount)
		{
			if (amount < 0)
			{
				throw new ArgumentLessThanZeroException();
			}

            // TODO #535: Move to DatadogService
            DogStatsd.Counter("currency.change", amount);

			user.Currency += amount;
        }

        /// <summary>
        /// Starts a transaction and removes an <paramref name="amount"/> of 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="amount"></param>
        /// <exception cref="ArgumentLessThanZeroException"></exception>
        /// <exception cref="InsufficientCurrencyException"></exception>
        /// <see cref="AddCurrency(User, int)"/>
        public static void RemoveCurrency(this User user, int amount)
		{
			if(amount < 0)
			{
				throw new ArgumentLessThanZeroException(); 
			}

			if(user.Currency < amount)
			{
				throw new InsufficientCurrencyException(user.Currency, amount);
			}

            // TODO #535: Move to DatadogService
            DogStatsd.Counter("currency.change", -amount);

			user.Currency -= amount;
		}
	}
}