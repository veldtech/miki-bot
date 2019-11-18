namespace Miki.Services.Blackjack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using API.Cards;
    using API.Cards.Enums;
    using API.Cards.Objects;
    using Patterns.Repositories;

    public class BlackjackService
    {
        private readonly IAsyncRepository<BlackjackContext> repository;

        private const ulong DealerId = 0;

        public BlackjackService(
            IAsyncRepository<BlackjackContext> repository)
        {
            this.repository = repository;
        }

        public async Task<BlackjackSession> CreateNewAsync(
            ulong messageId, ulong userId, ulong channelId, int bet)
        {
            var context = new BlackjackContext
            {
                Bet = bet,
                Deck = CardSet.CreateStandard(),
                Hands = new Dictionary<ulong, CardHand>
                {
                    { userId, new CardHand() },
                    { DealerId, new CardHand() }
                },
                ChannelId = channelId,
                UserId = userId,
                MessageId = messageId,
            };
            await repository.AddAsync(context);
            return new BlackjackSession(context);
        }

        /// <inheritdoc />
        public BlackjackState DrawCard(BlackjackSession session, ulong playerId)
        {
            if(!session.Players.TryGetValue(playerId, out var currentPlayer))
            {
                throw new InvalidOperationException();
            }
            currentPlayer.AddToHand(session.Deck.DrawRandom());
            if(session.GetHandWorth(currentPlayer) > 21)
            {
                return BlackjackState.LOSE;
            }
            return BlackjackState.NONE;
        }

        /// <inheritdoc />
        public BlackjackState Stand(BlackjackSession session, ulong playerId)
        {
            if(!session.Players.TryGetValue(playerId, out var currentPlayer))
            {
                throw new InvalidOperationException();
            }

            if(!session.Players.TryGetValue(DealerId, out var dealer))
            {
                throw new InvalidOperationException();
            }

            while(true)
            {
                if(session.GetHandWorth(dealer) >= Math.Max(session.GetHandWorth(currentPlayer), 17))
                {
                    if(currentPlayer.Hand.Count >= 5)
                    {
                        if(dealer.Hand.Count == 5)
                        {
                            if(session.GetHandWorth(dealer) == session.GetHandWorth(currentPlayer))
                            {
                                return BlackjackState.DRAW;
                            }

                            if(session.GetHandWorth(dealer) > session.GetHandWorth(currentPlayer))
                            {
                                return BlackjackState.LOSE;
                            }

                            return BlackjackState.WIN;
                        }
                    }
                    else
                    {
                        if(session.GetHandWorth(dealer) == session.GetHandWorth(currentPlayer))
                        {
                            return BlackjackState.DRAW;
                        }

                        return BlackjackState.LOSE;
                    }
                }

                dealer.AddToHand(session.Deck.DrawRandom());

                if(session.GetHandWorth(dealer) > 21)
                {
                    return BlackjackState.WIN;
                }
            }
        }

    }

    public class BlackjackSession
    {
        private readonly BlackjackContext context;
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
        private const ulong DealerId = 0;

        /// <inheritdoc />
        public int Bet => context.Bet;

        /// <inheritdoc />
        public IDictionary<ulong, CardHand> Players => context.Hands;

        /// <inheritdoc />
        public CardSet Deck => context.Deck;

        public BlackjackSession(BlackjackContext context)
        {
            this.context = context;
        }

        public void AttachMessage(ulong messageId)
        {
            if (context.MessageId != 0)
            {
                throw new InvalidOperationException("Cannot reset message.");
            }
            context.MessageId = messageId;
        }

        public int GetHandWorth(CardHand hand)
        {
            int aces = hand.Hand.Count(c => c.value == CardValue.ACES);
            int worth = hand.Hand.Sum(card => CardWorth[card.value]);

            while(worth > 21 && aces > 0)
            {
                worth -= 10;
                aces--;
            }

            return worth;
        }

        public override bool Equals(object obj)
        {
            if (obj is BlackjackSession session)
            {
                return session.context == context;
            }
            return false;
        }
    }

    public enum BlackjackState
    {
        NONE, WIN, LOSE, DRAW
    }
}