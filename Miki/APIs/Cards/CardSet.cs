using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;

namespace Miki.API.Cards
{
    public class CardSet
    {
        public List<Card> Deck = new List<Card>();

        public CardSet()
        {
            Deck.Add(new Card(CardType.CLUBS, CardValue.ACES));
            Deck.Add(new Card(CardType.CLUBS, CardValue.TWOS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.THREES));
            Deck.Add(new Card(CardType.CLUBS, CardValue.FOURS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.FIVES));
            Deck.Add(new Card(CardType.CLUBS, CardValue.SIXES));
            Deck.Add(new Card(CardType.CLUBS, CardValue.SEVENS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.EIGHTS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.NINES));
            Deck.Add(new Card(CardType.CLUBS, CardValue.TENS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.JACKS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.QUEENS));
            Deck.Add(new Card(CardType.CLUBS, CardValue.KINGS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.ACES));
            Deck.Add(new Card(CardType.HEARTS, CardValue.TWOS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.THREES));
            Deck.Add(new Card(CardType.HEARTS, CardValue.FOURS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.FIVES));
            Deck.Add(new Card(CardType.HEARTS, CardValue.SIXES));
            Deck.Add(new Card(CardType.HEARTS, CardValue.SEVENS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.EIGHTS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.NINES));
            Deck.Add(new Card(CardType.HEARTS, CardValue.TENS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.JACKS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.QUEENS));
            Deck.Add(new Card(CardType.HEARTS, CardValue.KINGS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.ACES));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.TWOS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.THREES));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.FOURS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.FIVES));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.SIXES));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.SEVENS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.EIGHTS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.NINES));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.TENS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.JACKS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.QUEENS));
            Deck.Add(new Card(CardType.DIAMONDS, CardValue.KINGS));
            Deck.Add(new Card(CardType.SPADES, CardValue.ACES));
            Deck.Add(new Card(CardType.SPADES, CardValue.TWOS));
            Deck.Add(new Card(CardType.SPADES, CardValue.THREES));
            Deck.Add(new Card(CardType.SPADES, CardValue.FOURS));
            Deck.Add(new Card(CardType.SPADES, CardValue.FIVES));
            Deck.Add(new Card(CardType.SPADES, CardValue.SIXES));
            Deck.Add(new Card(CardType.SPADES, CardValue.SEVENS));
            Deck.Add(new Card(CardType.SPADES, CardValue.EIGHTS));
            Deck.Add(new Card(CardType.SPADES, CardValue.NINES));
            Deck.Add(new Card(CardType.SPADES, CardValue.TENS));
            Deck.Add(new Card(CardType.SPADES, CardValue.JACKS));
            Deck.Add(new Card(CardType.SPADES, CardValue.QUEENS));
            Deck.Add(new Card(CardType.SPADES, CardValue.KINGS));
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
