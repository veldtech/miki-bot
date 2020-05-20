using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using StatsdClient;

namespace Miki.Helpers
{
    public static class DatabaseHelpers
	{
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