using Miki.API.Cards;
using Miki.API.Cards.Objects;
using ProtoBuf;
using System.Collections.Generic;

namespace Miki.API.Gambling
{
    // TODO: finish this
    internal class GamblingApi
    {
    }

    internal class GamblingSettings
    {
        public SlotsSettings slotSettings = new SlotsSettings();
    }

    internal class SlotsSettings
    {
        private List<SlotsItem> items = new List<SlotsItem>();

        public SlotsSettings AddItem(SlotsItem item)
        {
            items.Add(item);
            return this;
        }

        public SlotsSettings AddItem(string emoji, float weight)
        {
            return AddItem(new SlotsItem(emoji, weight));
        }
    }

	[ProtoContract]
	internal class BlackjackContext
	{
		[ProtoMember(1)]
		public int Bet;

		[ProtoMember(2)]
		public CardHand Player;

		[ProtoMember(3)]
		public CardHand Opponent;

		[ProtoMember(4)]
		public CardSet Deck;
	}

	internal class SlotsItem
    {
        public SlotsItem(string emoji, float weight)
        {
            Emoji = emoji;
            Weight = weight;
        }

        public string Emoji { get; set; }
        public float Weight { get; set; }
    }

    internal class SlotsResponse
    {
        private int MoneyBack { get; set; }
        private bool Win { get; set; }
    }
}