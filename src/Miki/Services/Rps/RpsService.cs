namespace Miki.Services.Rps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Miki.Framework;
    using Miki.Services.Transactions;
    using Miki.Utility;

    public enum VictoryStatus
    {
        DRAW = 0,
        WIN = 1,
        LOSE = 2,
    }

	public interface IRpsService
    {
        VictoryStatus CalculateVictory(RpsWeapon player, RpsWeapon cpu);
        
        IEnumerable<string> GetAllWeapons();
        
        RpsWeapon GetRandomWeapon();
        
        Task<RpsGameResult> PlayRpsAsync(long userId, int bet, string weapon);

    }
    public class RpsService : IRpsService
	{
        private readonly ITransactionService transactionService;
        private readonly List<RpsWeapon> weapons = new List<RpsWeapon>();

		public RpsService(
            ITransactionService transactionService)
		{
            this.transactionService = transactionService;

            weapons.Add(new RpsWeapon("scissors", ":scissors:"));
			weapons.Add(new RpsWeapon("paper", ":page_facing_up:"));
			weapons.Add(new RpsWeapon("rock", ":full_moon:"));
		}

		public IEnumerable<string> GetAllWeapons()
        {
            return weapons.Select(x => x.Name);
        }

		public RpsWeapon GetRandomWeapon()
		{
			return MikiRandom.Of(weapons);
		}
        
		public VictoryStatus CalculateVictory(RpsWeapon player, RpsWeapon cpu)
		{
			int playerIndex = weapons.IndexOf(player);
			int cpuIndex = weapons.IndexOf(cpu);
			return CalculateVictory(playerIndex, cpuIndex);
		}
        public VictoryStatus CalculateVictory(int player, int cpu)
        {
            return (VictoryStatus)((cpu - player + 3) % weapons.Count);
        }

        public async Task<RpsGameResult> PlayRpsAsync(long userId, int bet, string weapon)
        {
            if(!RpsWeapon.TryParse(weapon, out var playerWeapon))
            {
                // TODO: throw exception
            }

            await transactionService.CreateTransactionAsync(
                new TransactionRequest.Builder()
                    .WithReceiver(AppProps.Currency.BankId)
                    .WithSender(userId)
                    .WithAmount(bet)
                    .Build());
            RpsWeapon botWeapon = GetRandomWeapon();

            var status = CalculateVictory(playerWeapon, botWeapon);
            var builder = new RpsGameResult.Builder()
                .WithPlayerWeapon(playerWeapon)
                .WithCpuWeapon(botWeapon)
                .WithBet(bet)
                .WithStatus(status);

            switch(status)
            {
                case VictoryStatus.DRAW:
                    await transactionService.CreateTransactionAsync(
                        new TransactionRequest.Builder()
                            .WithAmount(bet)
                            .WithReceiver(userId)
                            .WithSender(AppProps.Currency.BankId)
                            .Build());
                    break;
                case VictoryStatus.WIN:
                    builder.WithAmountWon((int)(bet * 2.0) - bet);
                    await transactionService.CreateTransactionAsync(
                        new TransactionRequest.Builder()
                            .WithAmount((int)(bet * 2.0))
                            .WithReceiver(userId)
                            .WithSender(AppProps.Currency.BankId)
                            .Build());
                    break;
                case VictoryStatus.LOSE:
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
            private VictoryStatus status;
            private int? amountWon;
            private int bet;

            public Builder WithPlayerWeapon(RpsWeapon weapon)
            {
                playerWeapon = weapon;
                return this;
            }

            public Builder WithStatus(VictoryStatus status)
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

        public VictoryStatus Status { get; }

        /// <summary>
        /// Gets the amount won if the Status is WIN.
        /// </summary>
        public int? AmountWon { get; }

        public int Bet { get; }

        public RpsGameResult(
            RpsWeapon playerWeapon,
            RpsWeapon cpuWeapon,
            VictoryStatus status,
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