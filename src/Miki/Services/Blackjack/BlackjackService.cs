namespace Miki.Services.Blackjack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using API.Cards;
    using API.Cards.Enums;
    using API.Cards.Objects;
    using Miki.Framework;
    using Miki.Utility;
    using Patterns.Repositories;

    public class BlackjackService
    {
        private readonly IUnitOfWork unit;
        private readonly IAsyncRepository<BlackjackContext> repository;
        private readonly TransactionService transactionService;

        private const ulong DealerId = 0;

        public BlackjackService(
            IUnitOfWork unit,
            TransactionService transactionService,
            IRepositoryFactory<BlackjackContext> factory = null)
        {
            this.unit = unit;
            this.repository = unit.GetRepository(factory);
            this.transactionService = transactionService;
        }

        public async Task<BlackjackSession> NewSessionAsync(
            ulong messageId,
            ulong userId,
            ulong channelId,
            int bet)
        {
            return await transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(bet)
                        .WithReceiver((long)DealerId)
                        .WithSender((long)userId)
                        .Build())
                .Map(context => ConstructContext(bet, userId, channelId, messageId))
                    .AndThen(context => repository.AddAsync(context))
                    .AndThen(x => unit.CommitAsync())
                .Map(context => new BlackjackSession(context));
        }

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

        private BlackjackContext ConstructContext(
            int bet, ulong userId, ulong channelId, ulong messageId)
        {
            return new BlackjackContext
            {
                Bet = bet,
                Deck = CardSet.CreateStandard(),
                Hands = new Dictionary<ulong, CardHand>
                {
                    {userId, new CardHand()},
                    {DealerId, new CardHand()}
                },
                ChannelId = channelId,
                UserId = userId,
                MessageId = messageId,
            };
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

        public int Bet => context.Bet;

        public IDictionary<ulong, CardHand> Players => context.Hands;

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