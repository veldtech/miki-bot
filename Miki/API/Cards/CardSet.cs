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

		public CardSet()
		{
		}

		public static CardSet CreateStandard()
		{
			CardSet set = new CardSet();
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.ACES));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.TWOS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.THREES));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.FOURS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.FIVES));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.SIXES));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.SEVENS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.EIGHTS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.NINES));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.TENS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.JACKS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.QUEENS));
			set.Deck.Add(new Card(CardType.CLUBS, CardValue.KINGS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.ACES));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.TWOS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.THREES));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.FOURS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.FIVES));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.SIXES));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.SEVENS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.EIGHTS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.NINES));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.TENS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.JACKS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.QUEENS));
			set.Deck.Add(new Card(CardType.HEARTS, CardValue.KINGS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.ACES));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.TWOS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.THREES));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.FOURS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.FIVES));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.SIXES));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.SEVENS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.EIGHTS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.NINES));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.TENS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.JACKS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.QUEENS));
			set.Deck.Add(new Card(CardType.DIAMONDS, CardValue.KINGS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.ACES));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.TWOS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.THREES));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.FOURS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.FIVES));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.SIXES));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.SEVENS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.EIGHTS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.NINES));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.TENS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.JACKS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.QUEENS));
			set.Deck.Add(new Card(CardType.SPADES, CardValue.KINGS));
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