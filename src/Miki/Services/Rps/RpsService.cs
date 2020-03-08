namespace Miki.Services.Rps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Functional;
    using Miki.Services.Transactions;
    using Miki.Utility;

    public interface IRpsService
    {
        GameResult CalculateVictory(RpsWeapon player, RpsWeapon cpu);
        
        IReadOnlyList<string> GetAllWeapons();
        
        RpsWeapon GetRandomWeapon();

        /// <summary>
        /// Returns a valid weapon object which is
        /// </summary>
        /// <param name="weaponName">Query on the weapon's name.</param>
        /// <returns>The matching weapon type.</returns>
        Optional<RpsWeapon> GetWeapon(string weaponName);
        
        Task<RpsGameResult> PlayRpsAsync(long userId, int bet, string weapon);

    }
    public class RpsService : IRpsService
	{
        private readonly ITransactionService transactionService;

        /// <summary>
        /// Weapons cannot have the same first letter, because this will break
        /// <see cref="GetWeapon(string)"/>.
        /// </summary>
        private readonly List<RpsWeapon> weapons = new List<RpsWeapon>
        {
            new RpsWeapon("Rock", ":full_moon:"),
            new RpsWeapon("Scissors", ":scissors:"),
            new RpsWeapon("Paper", ":page_facing_up:")
        };

        public RpsService(
            ITransactionService transactionService)
		{
            this.transactionService = transactionService;
        }

		public IReadOnlyList<string> GetAllWeapons()
        {
            return weapons
                .Select(x => x.Name)
                .ToList();
        }

		public RpsWeapon GetRandomWeapon()
		{
			return MikiRandom.Of(weapons);
		}
        
		public GameResult CalculateVictory(RpsWeapon player, RpsWeapon cpu)
		{
			var playerIndex = weapons.IndexOf(player);
			var cpuIndex = weapons.IndexOf(cpu);
			return CalculateVictory(playerIndex, cpuIndex);
		}
        public GameResult CalculateVictory(int player, int cpu)
        {
            return (GameResult)((cpu - player + 3) % GetAllWeapons().Count);
        }

        public Optional<RpsWeapon> GetWeapon(string weaponName)
        {
            if(string.IsNullOrWhiteSpace(weaponName))
            {
                return Optional<RpsWeapon>.None;
            }
            return weapons.FirstOrDefault(
                x => char.ToLower(x.Name[0]) == char.ToLower(weaponName[0]));
        }

        public async Task<RpsGameResult> PlayRpsAsync(long userId, int bet, string weapon)
        {
            var playerWeapon = GetWeapon(weapon).Unwrap();
            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender(userId)
                    .WithAmount(bet)
                    .Build());
            var botWeapon = GetRandomWeapon();

            var status = CalculateVictory(playerWeapon, botWeapon);
            var builder = new RpsGameResult.Builder()
                .WithPlayerWeapon(playerWeapon)
                .WithCpuWeapon(botWeapon)
                .WithBet(bet)
                .WithStatus(status);

            switch(status)
            {
                case GameResult.Draw:
                    await transactionService.CreateTransactionAsync(
                        new TransactionRequest.Builder()
                            .WithAmount(bet)
                            .WithReceiver(userId)
                            .WithSender(AppProps.Currency.BankId)
                            .Build());
                    break;
                case GameResult.Win:
                    builder.WithAmountWon((int)(bet * 2.0) - bet);
                    await transactionService.CreateTransactionAsync(
                        new TransactionRequest.Builder()
                            .WithAmount((int)(bet * 2.0))
                            .WithReceiver(userId)
                            .WithSender(AppProps.Currency.BankId)
                            .Build());
                    break;
                case GameResult.Lose:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
             
            return builder.Build();
        }
    }

    public class RpsGameResult
    {
        public class Builder
        {
            private RpsWeapon cpuWeapon;
            private RpsWeapon playerWeapon;
            private GameResult status;
            private int? amountWon;
            private int bet;

            public Builder WithPlayerWeapon(RpsWeapon weapon)
            {
                playerWeapon = weapon;
                return this;
            }

            public Builder WithStatus(GameResult status)
            {
                this.status = status;
                return this;
            }

            public Builder WithAmountWon(int amountWon)
            {
                this.amountWon = amountWon;
                return this;
            }

            public Builder WithBet(int bet)
            {
                this.bet = bet;
                return this;
            }

            public Builder WithCpuWeapon(RpsWeapon weapon)
            {
                cpuWeapon = weapon;
                return this;
            }

            public RpsGameResult Build()
            {
                return new RpsGameResult(
                    playerWeapon, cpuWeapon, status, amountWon, bet);
            }
        }

        /// <summary>
        /// The bot's randomly generated weapon.
        /// </summary>
        public RpsWeapon CpuWeapon { get; }

        public RpsWeapon PlayerWeapon { get; }

        public GameResult Status { get; }

        /// <summary>
        /// Gets the amount won if the Status is WIN.
        /// </summary>
        public int? AmountWon { get; }

        public int Bet { get; }

        public RpsGameResult(
            RpsWeapon playerWeapon,
            RpsWeapon cpuWeapon,
            GameResult status,
            int? amountWon,
            int bet)
        {
            CpuWeapon = cpuWeapon;
            PlayerWeapon = playerWeapon;
            Status = status;
            AmountWon = amountWon;
            Bet = bet;
        }
    }
}