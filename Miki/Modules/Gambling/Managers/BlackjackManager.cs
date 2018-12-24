using Miki.API.Cards;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Framework.Events;
using Miki.Services.Blackjack.Exceptions;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules.Gambling.Managers
{
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
	}

	public class BlackjackManager : CardManager
	{
		public int Bet;
		public ulong MessageId;

		private static readonly Dictionary<CardValue, int> CardWorth = new Dictionary<CardValue, int>
		{
			{ CardValue.ACES,   11 },
			{ CardValue.TWOS,   2 },
			{ CardValue.THREES, 3 },
			{ CardValue.FOURS,  4 },
			{ CardValue.FIVES,  5 },
			{ CardValue.SIXES,  6 },
			{ CardValue.SEVENS, 7 },
			{ CardValue.EIGHTS, 8 },
			{ CardValue.NINES,  9 },
			{ CardValue.TENS,   10 },
			{ CardValue.JACKS,  10 },
			{ CardValue.QUEENS, 10 },
			{ CardValue.KINGS,  10 },
		};

		public BlackjackManager()
		{
		}

		public BlackjackManager(int bet)
		{
			Bet = bet;
			_deck = CardSet.CreateStandard();
		}

		public static BlackjackManager FromContext(BlackjackContext context)
		{
			if(context == null)
			{
				throw new ArgumentNullException();
			}

			BlackjackManager manager = new BlackjackManager();
			manager.Bet = context.Bet;
			manager.MessageId = context.MessageId;
			manager._deck = context.Deck;
			manager._hands = context.Hands;
			return manager;
		}

		public static async Task<BlackjackManager> FromCacheClientAsync(ICacheClient client, ulong channelId, ulong userId)
		{
			var context = await client.GetAsync<BlackjackContext>($"miki:blackjack:{channelId}:{userId}");

			if (context == null)
			{
				throw new BlackjackSessionNullException();
			}

			return FromContext(context);
		}

		public EmbedBuilder CreateEmbed(EventContext e)
		{
			string explanation = e.Locale.GetString("miki_blackjack_explanation");

			CardHand Player = GetPlayer(e.Author.Id);
			CardHand Dealer = GetPlayer(0);

			return new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = e.Locale.GetString("miki_blackjack") + " | " + e.Author.Username,
					IconUrl = e.Author.GetAvatarUrl(),
					Url = "https://patreon.com/mikibot"
				},
				Description = $"{explanation}\n{e.Locale.GetString("miki_blackjack_hit")}\n{e.Locale.GetString("miki_blackjack_stay")}"
			}.AddField(e.Locale.GetString("miki_blackjack_cards_you", Worth(Player)), Player.Print(), true)
			.AddField(e.Locale.GetString("miki_blackjack_cards_miki", Worth(Dealer)), Dealer.Print(), true);
		}

		public BlackjackContext ToContext()
		{
			return new BlackjackContext()
			{
				Bet = Bet,
				MessageId = MessageId,
				Deck = _deck,
				Hands = _hands
			};
		}

		public int Worth(CardHand hand)
		{
			int x = 0;
			int aces = hand.Hand.Count(c => c.value == CardValue.ACES);

			hand.Hand.ForEach(card =>
			{
				if (card.isPublic)
				{
					x += CardWorth[card.value];
				}
			});

			while (x > 21 && aces > 0)
			{
				x -= 10;
				aces--;
			}

			return x;
		}
	}
}