using Miki.API.Cards.Enums;
using ProtoBuf;
using System;

namespace Miki.API.Cards.Objects
{
	[ProtoContract]
	public class Card
	{
		[ProtoMember(1)]
		public CardType type;

		[ProtoMember(2)]
		public CardValue value;

		[ProtoMember(3)]
		public bool isPublic;

        public Card()
        { }
		public Card(CardType t, CardValue v)
		{
			type = t;
			value = v;
		}

		public override string ToString()
		{
			if (!isPublic)
			{
				return "??";
			}

			string output = $":{type.ToString().ToLowerInvariant()}:";

			switch(value)
			{
				case CardValue.ACES:
				{
					output += "A";
				}
				break;

				case CardValue.TWOS:
				{
					output += "2";
				}
				break;

				case CardValue.THREES:
				{
					output += "3";
				}
				break;

				case CardValue.FOURS:
				{
					output += "4";
				}
				break;

				case CardValue.FIVES:
				{
					output += "5";
				}
				break;

				case CardValue.SIXES:
				{
					output += "6";
				}
				break;

				case CardValue.SEVENS:
				{
					output += "7";
				}
				break;

				case CardValue.EIGHTS:
				{
					output += "8";
				}
				break;

				case CardValue.NINES:
				{
					output += "9";
				}
				break;

				case CardValue.TENS:
				{
					output += "10";
				}
				break;

				case CardValue.JACKS:
				{
					output += "J";
				}
				break;

				case CardValue.QUEENS:
				{
					output += "Q";
				}
				break;

				case CardValue.KINGS:
                {
                    output += "K";
                } break;

                default:
                {
                    throw new InvalidOperationException($"{value} is not a valid card");
                }
			}
			return output;
		}
	}
}