namespace Miki.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using API.Cards;
    using API.Cards.Enums;
    using API.Cards.Objects;
    using Miki.Cache;
    using Miki.Services.Transactions;
    using Miki.Utility;
    using Patterns.Repositories;

    public class BlackjackService
    {
        private readonly IAsyncRepository<BlackjackContext> repository;
        private readonly ITransactionService transactionService;

        public const ulong DealerId = 0;

        public BlackjackService(
            IExtendedCacheClient cache,
            ITransactionService transactionService)
        {
            this.repository = new BlackjackRepository(cache);
            this.transactionService = transactionService;
        }

        public async Task<BlackjackSession> NewSessionAsync(
            ulong messageId,
            ulong userId,
            ulong channelId,
            int bet)
        {
            return await AssertBlackjackSessionEmpty(userId, channelId)
                .Map(() => transactionService.CreateTransactionAsync(
                    new TransactionRequest.Builder()
                        .WithAmount(bet)
                        .WithReceiver((long)DealerId)
                        .WithSender((long)userId)
                        .Build()))
                .Map(context => ConstructContext(bet, userId, channelId, messageId))
                    .AndThen(AddSessionAsync)
                .Map(context => new BlackjackSession(context));
        }

        public Task<BlackjackSession> LoadSessionAsync(
            ulong userId,
            ulong channelId)
        {
            return repository.GetAsync(channelId, userId)
                .AndThen(session => RuntimeAssert.NotNull(
                    session, new BlackjackSessionNullException()))
                .Map(session => new BlackjackSession(session));
        }

        private async ValueTask AddSessionAsync(BlackjackContext ctx)
        {
            await repository.AddAsync(ctx);
        }

        public ValueTask SyncSessionAsync(BlackjackContext ctx)
        {
            return repository.EditAsync(ctx);
        }

        public ValueTask EndSessionAsync(BlackjackSession session)
        {
            if(session == null)
            {
                throw new BlackjackSessionNullException();
            }
            return repository.DeleteAsync(session.GetContext());
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
                // TODO: write test that makes player fail.
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

                DrawCard(session, DealerId);

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

        private async Task AssertBlackjackSessionEmpty(ulong userId, ulong channelId)
        {
            var session = await repository.GetAsync(channelId, userId);
            if(session != null)
            {
                throw new DuplicateSessionException();
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

        public int Bet => context.Bet;

        public IDictionary<ulong, CardHand> Players => context.Hands;

        public CardSet Deck => context.Deck;
        public ulong MessageId => context.MessageId;

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
            int aces = hand.Hand
                .Count(c => c.value == CardValue.ACES
                            && c.isPublic);
            int worth = hand.Hand
                .Sum(card => card.isPublic
                    ? CardWorth[card.value]
                    : 0);

            while(worth > 21 && aces > 0)
            {
                worth -= 10;
                aces--;
            }

            return worth;
        }

        public BlackjackContext GetContext()
        {
            return context;
        }

        public override bool Equals(object obj)
        {
            if (obj is BlackjackSession session)
            {
                return session.context == context;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                context.UserId,
                context.MessageId,
                context.ChannelId);
        }
    }

    public enum BlackjackState
    {
        NONE, 
        WIN, 
        LOSE, 
        DRAW
    }
}