using Miki.Localization;
using Miki.Extensions;
using Miki.Services.Rps;
using System;

namespace Miki.Modules.Gambling.Resources
{
    public class GameResultResource : IResource
    {
        private readonly GameResult result;
        private readonly int currency;
        private readonly int bet;
        private readonly int winnings;

        public GameResultResource(GameResult result, int currency, int bet, int winnings = 0)
        {
            this.result = result;
            this.currency = currency;
            this.bet = bet;
            this.winnings = winnings;
        }

        /// <inheritdoc />
        public string Get(IResourceManager instance)
        {
            switch(result)
            {
                case GameResult.Win:
                    return instance.GetString("game_result_win", $"**{winnings:N0}**")
                           + " "
                           + instance.GetString("currency_update_balance", 
                               $"**{currency + winnings:N0}**");

                case GameResult.Lose:
                    return instance.GetString("game_result_lose", $"**{bet:N0}**")
                           + " "
                           + instance.GetString("currency_update_balance", $"**{currency - bet:N0}**");

                case GameResult.Draw:
                    return instance.GetString("game_result_draw");
            }

            throw new InvalidOperationException("Invalid result state");
        }
    }
}
