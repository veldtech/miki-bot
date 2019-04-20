using Miki.API.Cards.Objects;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Miki.API.Cards
{
	public delegate int GetCardValue(int totalValue, CardHand hand);

	[ProtoContract]
	public class CardManager
	{
		[ProtoMember(1)]
		protected Dictionary<ulong, CardHand> _hands = new Dictionary<ulong, CardHand>();

		[ProtoMember(2)]
		protected CardSet _deck = new CardSet();

		public CardHand AddPlayer(ulong userid)
		{
			if (!_hands.ContainsKey(userid))
			{
				CardHand hand = new CardHand();
				_hands.Add(userid, hand);
				return hand;
			}
			return null;
		}

		public CardHand GetPlayer(ulong userId)
		{
			_hands.TryGetValue(userId, out CardHand value);
			return value;
		}

		public void DealAll()
		{
			foreach (CardHand h in _hands.Values)
			{
				h.AddToHand(_deck.DrawRandom());
			}
		}

		public void DealTo(ulong userid)
		{
			if (_hands.TryGetValue(userid, out CardHand hand))
			{
				DealTo(hand);
			}
			throw new InvalidOperationException("Your hand does not exist.");
		}

		public void DealTo(CardHand hand)
		{
			hand.AddToHand(_deck.DrawRandom());
		}
	}
}