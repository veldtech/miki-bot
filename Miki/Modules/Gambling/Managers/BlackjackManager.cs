using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Miki.API.Cards;
using Miki.API.Cards.Enums;
using Miki.API.Cards.Objects;
using Miki.Framework.Events;
using Miki.Discord.Common;
using Miki.Discord;

namespace Miki.Modules.Gambling.Managers
{
    public class BlackjackManager : CardManager
    {
        public CardHand player = new CardHand();
        public CardHand dealer = new CardHand();

        public CardSet deck = new CardSet();

        public Dictionary<CardValue, GetCardValue> CardWorth = new Dictionary<CardValue, GetCardValue>();

        public BlackjackManager()
        {
            CardWorth.Add(CardValue.ACES,   (x, hand) => 11);
            CardWorth.Add(CardValue.TWOS,   (x, hand) => 2);
            CardWorth.Add(CardValue.THREES, (x, hand) => 3);
            CardWorth.Add(CardValue.FOURS,  (x, hand) => 4);
            CardWorth.Add(CardValue.FIVES,  (x, hand) => 5);
            CardWorth.Add(CardValue.SIXES,  (x, hand) => 6);
            CardWorth.Add(CardValue.SEVENS, (x, hand) => 7);
            CardWorth.Add(CardValue.EIGHTS, (x, hand) => 8);
            CardWorth.Add(CardValue.NINES,  (x, hand) => 9);
            CardWorth.Add(CardValue.TENS,   (x, hand) => 10);
            CardWorth.Add(CardValue.JACKS,  (x, hand) => 10);
            CardWorth.Add(CardValue.QUEENS, (x, hand) => 10);
            CardWorth.Add(CardValue.KINGS,  (x, hand) => 10);

            player.AddToHand(deck.DrawRandom());
            player.AddToHand(deck.DrawRandom());

            dealer.AddToHand(deck.DrawRandom());
            dealer.AddToHand(deck.DrawRandom(false));
        }

		public EmbedBuilder CreateEmbed(EventContext e)
		{
			string explanation = e.GetResource("miki_blackjack_explanation");

			return new EmbedBuilder()
			{
				Author = new EmbedAuthor()
				{
					Name = e.GetResource("miki_blackjack") + " | " + e.Author.Username,
					IconUrl = e.Author.GetAvatarUrl(),
					Url = "https://patreon.com/mikibot"
				},
				Description = $"{explanation}\n{e.GetResource("miki_blackjack_hit")}\n{e.GetResource("miki_blackjack_stay")}"
			}.AddField(e.GetResource("miki_blackjack_cards_you", Worth(player)), player.Print(), true)
			.AddField(e.GetResource("miki_blackjack_cards_miki", Worth(dealer)), dealer.Print(), true);
		}

        public int Worth(CardHand hand)
        {
            int x = 0;
            int aces = hand.Hand.Count(c => c.value == CardValue.ACES);

            hand.Hand.ForEach(card =>
            {
                if (card.isPublic)
                {
                    x += CardWorth[card.value](x, hand);
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
