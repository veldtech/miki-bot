namespace Miki.Services
{
    using System.Collections.Generic;
    using API.Cards;
    using API.Cards.Objects;
    using ProtoBuf;

    [ProtoContract]
	public class BlackjackContext
	{
        [ProtoMember(1)]
		public int Bet;

		[ProtoMember(2)]
		public ulong MessageId;

        [ProtoMember(3)]
		public Dictionary<ulong, CardHand> Hands = new Dictionary<ulong, CardHand>();

		[ProtoMember(4)]
		public CardSet Deck = new CardSet();

        [ProtoIgnore] public ulong ChannelId;
        [ProtoIgnore] public ulong UserId;
    }
}