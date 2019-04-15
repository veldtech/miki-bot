using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Miki.Modules.Gambling.Services.Roulette.Models
{
    public enum InsideBetTypes
    {
        STRAIGHT,
        SPLIT,
        STREET,
        CORNER,
        FIVE,
        SIX
    }

    public enum OutsideBetTypes
    {
        RED, BLACK,
        ODD, EVEN,
        LOW, HIGH,
        COLUMN1, COLUMN2, COLUMN3,
        FIRST, SECOND, THIRD
    }

    [ProtoContract]
    public class RouletteBet
    {
        [ProtoMember(1)]
        public ulong UserId { get; set; }

        [ProtoMember(2)]
        public int BetAmount { get; set; }

        [ProtoMember(3)]
        public bool IsInside { get; set; }

        [ProtoMember(4)]
        public int BetType { get; set; }

        [ProtoMember(5)]
        public IEnumerable<int> NumbersAffected { get; set; }

        /// <summary>
        /// Decides whether the player has won or not based on <paramref name="resultNum"/>.
        /// </summary>
        public bool HasWon(int resultNum)
        {
            if (IsInside)
            {
                return NumbersAffected
                    .Any(x => x == resultNum);
            }
            else
            {
                var type = (OutsideBetTypes)BetType;
                switch (type)
                {
                    case OutsideBetTypes.BLACK:
                        return new int[]
                        {
                            2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35
                        }.Any(x => x == resultNum);
                    case OutsideBetTypes.RED:
                        return new int[]
                        {
                            1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
                        }.Any(x => x == resultNum);
                    case OutsideBetTypes.COLUMN1:
                        return new int[]
                        {
                            1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34
                        }.Any(x => x == resultNum);
                    case OutsideBetTypes.COLUMN2:
                        return new int[]
                        {
                            2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35
                        }.Any(x => x == resultNum);
                    case OutsideBetTypes.COLUMN3:
                        return new int[]
                        {
                            3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36
                        }.Any(x => x == resultNum);
                    case OutsideBetTypes.EVEN:
                        return resultNum % 2 == 0;
                    case OutsideBetTypes.ODD:
                        return resultNum % 2 != 0;
                    case OutsideBetTypes.LOW:
                        return resultNum <= 18;
                    case OutsideBetTypes.HIGH:
                        return resultNum > 18;
                    case OutsideBetTypes.FIRST:
                        return resultNum < 13;
                    case OutsideBetTypes.SECOND:
                        return resultNum > 12 && resultNum <= 24;
                    case OutsideBetTypes.THIRD:
                        return resultNum > 24;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Returns amount won, but does not check if the player won. Use <see cref="HasWon(int)"/> to find out if the player won.
        /// </summary>
        public int GetWinAmount()
        {
            if(IsInside)
            {
                var type = (InsideBetTypes)BetType;

                switch (type)
                {
                    case InsideBetTypes.STRAIGHT:
                    {
                        return BetAmount * 35;
                    }

                    case InsideBetTypes.SPLIT:
                    {
                        return BetAmount * 17;
                    }

                    case InsideBetTypes.CORNER:
                    {
                        return BetAmount * 11;
                    }

                    case InsideBetTypes.STREET:
                    {
                        return BetAmount * 8;
                    }

                    case InsideBetTypes.FIVE:
                    {
                        return BetAmount * 6;
                    }

                    case InsideBetTypes.SIX:
                    {
                        return BetAmount * 5;
                    }

                    default:
                    {
                        return BetAmount;
                    }
                }
            }
            else
            {
                var type = (OutsideBetTypes)BetType;

                switch (type)
                {
                    default:
                    case OutsideBetTypes.EVEN:
                    case OutsideBetTypes.ODD:
                    case OutsideBetTypes.RED:
                    case OutsideBetTypes.BLACK:
                    case OutsideBetTypes.LOW:
                    case OutsideBetTypes.HIGH:
                    {
                        return BetAmount;
                    }
                    case OutsideBetTypes.COLUMN1:
                    case OutsideBetTypes.COLUMN2:
                    case OutsideBetTypes.COLUMN3:
                    case OutsideBetTypes.FIRST:
                    case OutsideBetTypes.SECOND:
                    case OutsideBetTypes.THIRD:
                    {
                        return 2 * BetAmount;
                    }
                }
            }
        }

    }
}
