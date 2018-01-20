using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards.Enums;

namespace Miki.API.Cards.Objects
{
    public class Card
    {
        public CardType type;
        public CardValue value;
        public bool isPublic = true;

        public Card(CardType t, CardValue v)
        {
            type = t;
            value = v;
        }

        public string Print()
        {
            string output = "";

            if (!isPublic)
            {
                return "??";
            }

            output += ":" + type.ToString().ToLower() + ":";

            switch (value)
            {
                case CardValue.ACES:
                    output += "A";
                    break;
                case CardValue.TWOS:
                    output += "2";
                    break;
                case CardValue.THREES:
                    output += "3";
                    break;
                case CardValue.FOURS:
                    output += "4";
                    break;
                case CardValue.FIVES:
                    output += "5";
                    break;
                case CardValue.SIXES:
                    output += "6";
                    break;
                case CardValue.SEVENS:
                    output += "7";
                    break;
                case CardValue.EIGHTS:
                    output += "8";
                    break;
                case CardValue.NINES:
                    output += "9";
                    break;
                case CardValue.TENS:
                    output += "10";
                    break;
                case CardValue.JACKS:
                    output += "J";
                    break;
                case CardValue.QUEENS:
                    output += "Q";
                    break;
                case CardValue.KINGS:
                    output += "K";
                    break;
            }
            return output;
        }
    }
}
