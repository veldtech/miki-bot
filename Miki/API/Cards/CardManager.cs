using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards.Objects;
using Miki.Modules;

namespace Miki.API.Cards
{
    public delegate int GetCardValue(int totalValue, CardHand hand);

    public class CardManager
    {
        private Dictionary<ulong, CardHand> hands = new Dictionary<ulong, CardHand>();
        private CardSet deck = new CardSet();

        public void AddPlayer(ulong userid)
        {
            if (!hands.ContainsKey(userid))
            {
                hands.Add(userid, new CardHand());
            }
        }

        public void DealAll()
        {
            foreach (CardHand h in hands.Values)
            {
                h.AddToHand(deck.DrawRandom());
            }
        }

        public void DealTo(ulong userid)
        {
            hands[userid].AddToHand(deck.DrawRandom());
        }
    }
}
