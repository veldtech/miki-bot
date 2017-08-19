using IA;
using IA.Events;
using IA.Events.Attributes;
using IA.SDK;
using IA.SDK.Events;
using IA.SDK.Interfaces;
using Miki.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules
{
    [Module("Games")]
    public class GamesModule
    {
        [Command(Name = "blackjack", Aliases = new string[]{"bj"})]
        public async Task BlackjackAsync(EventContext e)
        {
            Locale locale = e.Channel.GetLocale();

            if (Bot.instance.Events.PrivateCommandHandlerExist(e.Author.Id, e.Channel.Id))
            {
                await e.ErrorEmbed(e.GetResource("blackjack_error_instance_exists"))
                    .SendToChannel(e.Channel);

                return;
            }

			await ValidateBet( e, StartBlackjack );
        }

		public async Task StartBlackjack( EventContext e, int bet )
        {
			using( var context = new MikiContext() )
			{
				User user = await context.Users.FindAsync( e.Author.Id.ToDbLong() );
				await user.RemoveCurrencyAsync( context, null, bet );
			}

			BlackjackManager bm = new BlackjackManager();

			IDiscordMessage message = await bm.CreateEmbed( e ).SendToChannel( e.Channel );

			CommandHandler c = new CommandHandlerBuilder( Bot.instance.Events )
				.AddPrefix( "" )
				.SetOwner( e.message )
				.AddCommand(
				new RuntimeCommandEvent( "hit" )
					.Default( async ( ec ) =>
					{
						await ec.message.DeleteAsync();

						bm.player.AddToHand( bm.deck.DrawRandom() );

						if( bm.Worth( bm.player ) > 21 )
						{
							await OnBlackjackDead( ec, bm, message, bet );
						}
						else
						{
							await bm.CreateEmbed( e ).ModifyMessage( message );
						}
					} ) )
				.AddCommand(
				new RuntimeCommandEvent( "stand" )
					.SetAliases( "knock", "stay", "stop" )
					.Default( async ( ec ) =>
					{
						bm.dealer.Hand.ForEach( x => x.isPublic = true );

						await ec.message.DeleteAsync();
						bool dealerQuit = false;

						while( !dealerQuit )
						{
							if( bm.Worth( bm.dealer ) >= bm.Worth( bm.player ) )
							{
								if( bm.Worth( bm.dealer ) == bm.Worth( bm.player ) )
								{
									await OnBlackjackDraw( ec, bm, message, bet );
								}
								else
								{
									await OnBlackjackDead( ec, bm, message, bet );
								}
								dealerQuit = true;
							}
							else
							{
								bm.dealer.AddToHand( bm.deck.DrawRandom() );
								if( bm.Worth( bm.dealer ) > 21 )
								{
									await OnBlackjackWin( ec, bm, message, bet );
									dealerQuit = true;
								}
							}
							await Task.Delay( 500 );
						}
					} ) ).Build();

			Bot.instance.Events.AddPrivateCommandHandler( e.message, c );

		}

        [Command(Name = "slots", Aliases = new string[] { "s" })]
		public async Task SlotsAsync( EventContext e ) 
		{
			await ValidateBet( e, StartSlots );
		}


		public async Task StartSlots(EventContext e, int bet)
        {
            int moneyBet = bet;

            using (var context = new MikiContext())
            {
                User u = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                int moneyReturned = 0;

                if (moneyBet <= 0)
                {
                    return;
                }

                string[] objects =
                {
                    "🍒", "🍒", "🍒", "🍒",
                    "🍊", "🍊",
                    "🍓", "🍓",
                    "🍍","🍍",
                    "🍇", "🍇",
                    "⭐", "⭐",
                    "🍍", "🍍",
                    "🍓", "🍓",
                    "🍊", "🍊", "🍊",
                    "🍒", "🍒", "🍒", "🍒",
                };

                IDiscordEmbed embed = Utils.Embed;
                embed.Title = locale.GetString(Locale.SlotsHeader);

                string[] objectsChosen =
                {
                    objects[MikiRandom.Next(objects.Length)],
                    objects[MikiRandom.Next(objects.Length)],
                    objects[MikiRandom.Next(objects.Length)]
                };

                Dictionary<string, int> score = new Dictionary<string, int>();

                foreach (string o in objectsChosen)
                {
                    if (score.ContainsKey(o))
                    {
                        score[o]++;
                        continue;
                    }
                    score.Add(o, 1);
                }

                if (score.ContainsKey("🍒"))
                {
                    if (score["🍒"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.5f);
                    }
                    else if (score["🍒"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                }
                if (score.ContainsKey("🍊"))
                {
                    if (score["🍊"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 0.8f);
                    }
                    else if (score["🍊"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1.5f);
                    }
                }
                if (score.ContainsKey("🍓"))
                {
                    if (score["🍓"] == 2)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 1f);
                    }
                    else if (score["🍓"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 2f);
                    }
                }
                if (score.ContainsKey("🍍"))
                {
                    if (score["🍍"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 4f);
                    }
                }
                if (score.ContainsKey("🍇"))
                {
                    if (score["🍇"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 6f);
                    }
                }
                if (score.ContainsKey("⭐"))
                {
                    if (score["⭐"] == 3)
                    {
                        moneyReturned = (int)Math.Ceiling(moneyBet * 12f);
                    }
                }

                if (moneyReturned == 0)
                {
					moneyReturned = -moneyBet;
					embed.AddField(locale.GetString("miki_module_fun_slots_lose_header"), locale.GetString( "miki_module_fun_slots_lose_amount", moneyBet, u.Currency - moneyBet ));
				}
                else
                {
					embed.AddField(locale.GetString(Locale.SlotsWinHeader), locale.GetString(Locale.SlotsWinMessage, moneyReturned, u.Currency + moneyReturned));
				}

                embed.Description = string.Join(" ", objectsChosen);
                await u.AddCurrencyAsync(moneyReturned, e.Channel);
                await context.SaveChangesAsync();

                await embed.SendToChannel(e.Channel);
            }
        }

        private async Task OnBlackjackDraw(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage, int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
			using (var context = new MikiContext())
            {
                user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
                if (user != null)
                {
                    await user.AddCurrencyAsync(bet, e.Channel);
                    await context.SaveChangesAsync();
                }
            }

            await bm.CreateEmbed(e)
                .SetTitle(e.GetResource("blackjack_draw_title"))
                .SetDescription( e.GetResource("blackjack_draw_description" ) + "\n" + e.GetResource( "miki_blackjack_current_balance", user.Currency ) )
                .ModifyMessage(instanceMessage);
        }

        private async Task OnBlackjackDead(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage, int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
			using( var context = new MikiContext() )
			{
				user = await context.Users.FindAsync( e.Author.Id.ToDbLong() );
			}

			await bm.CreateEmbed(e)
                .SetTitle(e.GetResource("miki_blackjack_lose_title"))
                .SetDescription(e.GetResource("miki_blackjack_lose_description") + "\n" + e.GetResource( "miki_blackjack_new_balance", user.Currency ) )
                .ModifyMessage(instanceMessage);
        }

        private async Task OnBlackjackWin(EventContext e, BlackjackManager bm, IDiscordMessage instanceMessage, int bet)
        {
            await e.commandHandler.RequestDisposeAsync();

            User user;
			using( var context = new MikiContext() )
			{
				user = await context.Users.FindAsync( e.Author.Id.ToDbLong() );
				if( user != null )
				{
					await user.AddCurrencyAsync(bet * 2, e.Channel);
					await context.SaveChangesAsync();
				}
			}

			await bm.CreateEmbed(e)
                .SetTitle(e.GetResource("miki_blackjack_win_title"))
                .SetDescription( e.GetResource("miki_blackjack_win_description", bet *2 ) + "\n" + e.GetResource( "miki_blackjack_new_balance", user.Currency ) )
                .ModifyMessage(instanceMessage);
        }

		public async Task ValidateBet( EventContext e, Func<EventContext, int, Task> callback = null )
		{
			if( !string.IsNullOrEmpty( e.arguments ) )
			{
				int bet = 0;
				int noAskLimit = 10000;

				using( var context = new MikiContext() )
				{
					User user = await context.Users.FindAsync( e.Author.Id.ToDbLong() );

					if( int.TryParse( e.arguments, out bet ) )
					{

					}
					else if( e.arguments.Contains( "all" ) || e.arguments.Contains( "*" ) )
					{
						bet = user.Currency;
					}
					else
					{
						await e.ErrorEmbed( e.GetResource( "miki_error_gambling_parse_error" ) )
							.SendToChannel( e.Channel );
						return;
					}

					if( bet < 1 )
					{
						await e.ErrorEmbed( e.GetResource( "miki_error_gambling_zero_or_less" ) )
							.SendToChannel( e.Channel );
						return;
					}
					else if( bet > user.Currency )
					{
						await e.ErrorEmbed( e.GetResource( "miki_mekos_insufficient" ) )
							.SendToChannel( e.Channel );
						return;
					}
					else if( bet >= noAskLimit )
					{
						IDiscordEmbed embed = Utils.Embed;
						embed.Description = $"Are you sure you want to bet **{bet}**? You currently have `{user.Currency}` mekos.\n\nType `>yes` to confirm.";
						embed.Color = new IA.SDK.Color( 0.4f, 0.6f, 1f );
						await embed.SendToChannel( e.Channel );

						CommandHandler confirmCommand = new CommandHandlerBuilder( Bot.instance.Events )
						.AddPrefix( "" )
						.DisposeInSeconds( 20 )
						.SetOwner( e.message )
						.AddCommand(
							new RuntimeCommandEvent( "yes" )
							.Default( async ( ec ) =>
							{
								await ec.commandHandler.RequestDisposeAsync();
								await ec.message.DeleteAsync();
								await callback( e, bet );
							} ) ).Build();

						Bot.instance.Events.AddPrivateCommandHandler( e.message, confirmCommand );
					}
					else
					{
						await callback( e, bet );
					}
				}
			}
			else
			{
				await Utils.ErrorEmbed( e.Channel.GetLocale(), e.GetResource( "miki_error_gambling_no_arg" ) )
					.SendToChannel( e.Channel );
				return;
			}

		}
	}

    public class CardManager
    {
        Dictionary<ulong, CardHand> hands = new Dictionary<ulong, CardHand>();

        CardSet deck = new CardSet();

        public void AddPlayer(ulong userid)
        {
            if(!hands.ContainsKey(userid))
            {
                hands.Add(userid, new CardHand());
            }   
        }

        public void DealAll()
        {
            foreach(CardHand h in hands.Values)
            {
                h.AddToHand(deck.DrawRandom());
            }
        }

        public void DealTo(ulong userid)
        {
            hands[userid].AddToHand(deck.DrawRandom());
        }
    }

    public class BlackjackManager : CardManager
    {
        public CardHand player = new CardHand();
        public CardHand dealer = new CardHand();

        public CardSet deck = new CardSet();

        public Dictionary<CardValue, GetCardValue> CardWorth = new Dictionary<CardValue, GetCardValue>();

        public BlackjackManager()
        {
            CardWorth.Add(CardValue.ACES,   (x) => (    x > 10) ? 1 : 11);
            CardWorth.Add(CardValue.TWOS,   (x) => 2);
            CardWorth.Add(CardValue.THREES, (x) => 3);
            CardWorth.Add(CardValue.FOURS,  (x) => 4);
            CardWorth.Add(CardValue.FIVES,  (x) => 5);
            CardWorth.Add(CardValue.SIXES,  (x) => 6);
            CardWorth.Add(CardValue.SEVENS, (x) => 7);
            CardWorth.Add(CardValue.EIGHTS, (x) => 8);
            CardWorth.Add(CardValue.NINES,  (x) => 9);
            CardWorth.Add(CardValue.TENS,   (x) => 10);
            CardWorth.Add(CardValue.JACKS,  (x) => 10);
            CardWorth.Add(CardValue.QUEENS, (x) => 10);
            CardWorth.Add(CardValue.KINGS,  (x) => 10);

            player.AddToHand(deck.DrawRandom());
            player.AddToHand(deck.DrawRandom());

            dealer.AddToHand(deck.DrawRandom());
            dealer.AddToHand(deck.DrawRandom(false));
        }

        public IDiscordEmbed CreateEmbed(EventContext e)
        {
            return Utils.Embed
                    .SetTitle(e.GetResource("miki_blackjack") + " " + e.Author.Username)
                    .SetDescription(e.GetResource("miki_blackjack_explanation") + "\n" + e.GetResource("miki_blackjack_hit") + "\n" + e.GetResource("miki_blackjack_stay"))
                    .AddInlineField(e.GetResource("miki_blackjack_cards_you", Worth(player)), player.Print())
                    .AddInlineField(e.GetResource("miki_blackjack_cards_miki", Worth(dealer)), dealer.Print());
        }

        public int Worth(CardHand hand)
        {
            int x = 0;
            hand.Hand.ForEach(card => {
                if (card.isPublic)
                {
                    x += CardWorth[card.value](x);
                }
            });
            return x;
        }
    }

    public class CardHand
    {
        public List<Card> Hand = new List<Card>();
        
        public void AddToHand(Card card)
        {
            Hand.Add(card);
            Hand.Sort((x, y) => ((y.isPublic) ? y.value : 0) - ((x.isPublic) ? x.value : 0));
        }

        public string Print()
        {
            string output = "";
            Hand.ForEach((x) => output += x.Print());
            return output;
        }
    }

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

    public class Card
    {
        public CardType type;
        public CardValue value;
        public bool isPublic = true;

        public Card(CardType t, CardValue v)
        {
            type = t;
            value = v;
        }

        public string Print()
        {
            string output = "";

            if(!isPublic)
            {
                return "??";
            }

            output += ":" + type.ToString().ToLower() + ":";

            switch (value)
            {
                case CardValue.ACES:
                    output += "A";
                    break;
                case CardValue.TWOS:
                    output += "2";
                    break;
                case CardValue.THREES:
                    output += "3";
                    break;
                case CardValue.FOURS:
                    output += "4";
                    break;
                case CardValue.FIVES:
                    output += "5";
                    break;
                case CardValue.SIXES:
                    output += "6";
                    break;
                case CardValue.SEVENS:
                    output += "7";
                    break;
                case CardValue.EIGHTS:
                    output += "8";
                    break;
                case CardValue.NINES:
                    output += "9";
                    break;
                case CardValue.TENS:
                    output += "10";
                    break;
                case CardValue.JACKS:
                    output += "J";
                    break;
                case CardValue.QUEENS:
                    output += "Q";
                    break;
                case CardValue.KINGS:
                    output += "K";
                    break;
            }
            return output;
        }
    }

    public enum CardType
    {
        HEARTS,   // :hearts:
        CLUBS,    // :clubs:
        DIAMONDS, // :diamonds:
        SPADES    // :spades:
    }

    public enum CardValue
    {
        ACES,     // A
        TWOS,     // 2
        THREES,   // 3
        FOURS,    // 4
        FIVES,    // 5
        SIXES,    // 6
        SEVENS,   // 7
        EIGHTS,   // 8
        NINES,    // 9
        TENS,     // 10
        JACKS,    // J
        QUEENS,   // Q
        KINGS     // K
    }

    public delegate int GetCardValue(int totalValue);
}