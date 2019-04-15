using Miki.Framework.Arguments;
using Miki.Modules.Gambling.Services.Roulette.Exceptions;
using Miki.Modules.Gambling.Services.Roulette.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miki.Modules.Gambling.Services.Roulette.Parsers
{
    public class RouletteBetArgumentParser : IArgumentParser
    {
        public Type OutputType => typeof(RouletteBet);

        public int Priority => 1;

        public bool CanParse(IArgumentPack pack)
        {
            switch (pack.Peek().ToLowerInvariant())
            {
                case "first":
                case "second":
                case "third":
                case "black":
                case "red":
                case "even":
                case "odd":
                case "high":
                case "low":
                case "column1":
                case "column2":
                case "column3":
                    return true;
                default:
                    return pack.Peek()
                        .Split(':')
                        .All(x => int.TryParse(x, out _));
            }

        }

        public object Parse(IArgumentPack pack)
        {
            RouletteBet bet = new RouletteBet();
            var arg = pack.Take();
            bet.NumbersAffected = arg.Split(':')
                .Select(x => int.Parse(x));

            switch (bet.NumbersAffected.Count())
            {
                case 0:
                {
                    bet.IsInside = false;
                    if(!Enum.TryParse<OutsideBetTypes>(arg, out var type))
                    {
                        throw new InvalidBetException();
                    }
                    bet.BetType = (int)type;
                } break;

                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                {
                    bet.IsInside = true;
                    bet.BetType = bet.NumbersAffected.Count() - 1;
                } break;

                default:
                    throw new InvalidBetException();
            }

            return bet;
        }
    }
}
