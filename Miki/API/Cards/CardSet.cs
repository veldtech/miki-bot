using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using ProtoBuf;

namespace Miki.API.Cards
{
	[ProtoContract]
	public class CardSet
	{
		[ProtoMember(1)]
		public List<Card> Deck = new List<Card>();

		public static CardSet CreateStandard()
		{
			CardSet set = new CardSet();

			foreach (CardType type in Enum.GetValues(typeof(CardType)))
			{
				foreach(CardValue value in Enum.GetValues(typeof(CardValue)))
				{
					set.Deck.Add(new Card(type, value));
				}
			}

			return set;
		}

		public Card DrawRandom(bool isPublic = true)
		{
			int rn = MikiRandom.Next(0, Deck.Count);
			Card card = Deck[rn];
			Deck.RemoveAt(rn);
			card.isPublic = isPublic;
			return card;
		}
	}
}