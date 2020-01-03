namespace Miki.API.Cards
{
    using Miki.API.Cards.Objects;
    using ProtoBuf;
    using System;
    using System.Collections.Generic;
    
    public delegate int GetCardValue(int totalValue, CardHand hand);

	[ProtoContract]
	public class CardManager
	{
		[ProtoMember(1)]
		protected Dictionary<ulong, CardHand> hands = new Dictionary<ulong, CardHand>();

		[ProtoMember(2)]
		protected CardSet deck = new CardSet();

		public CardHand AddPlayer(ulong userid)
		{
			if (!hands.ContainsKey(userid))
			{
				CardHand hand = new CardHand();
				hands.Add(userid, hand);
				return hand;
			}
			return null;
		}

		public CardHand GetPlayer(ulong userId)
		{
			hands.TryGetValue(userId, out CardHand value);
			return value;
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
			if (hands.TryGetValue(userid, out CardHand hand))
			{
				DealTo(hand);
			}
			throw new InvalidOperationException("Your hand does not exist.");
		}

		public void DealTo(CardHand hand)
		{
			hand.AddToHand(deck.DrawRandom());
		}
	}
}