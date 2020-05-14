using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using ProtoBuf;
using System;
using System.Collections.Generic;
using Miki.Utility;

namespace Miki.API.Cards
{
    [ProtoContract]
	public class CardSet : List<Card>
	{
		public static CardSet CreateStandard()
		{
			CardSet set = new CardSet();

			foreach (CardType type in Enum.GetValues(typeof(CardType)))
			{
				foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
				{
					set.Add(new Card(type, value));
				}
			}
            return set;
		}

		public Card DrawRandom(bool isPublic = true)
		{
			int rn = MikiRandom.Next(0, Count);
			Card card = this[rn];
			this.RemoveAt(rn);
			card.isPublic = isPublic;
			return card;
		}
    }
}