namespace Miki.Services.Marriage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Miki.Bot.Models;
    using Miki.Framework;
    using Miki.Patterns.Repositories;

    public class MarriageService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IAsyncRepository<Marriage> marriageRepository;
        private readonly IAsyncRepository<UserMarriedTo> marriedToRepository;

        public MarriageService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.marriageRepository = unitOfWork.GetRepository<Marriage>();
            this.marriedToRepository = unitOfWork.GetRepository<UserMarriedTo>();
        }

        public async Task DeclineAllProposalsAsync(long me)
        {
            List<UserMarriedTo> proposals = await InternalGetProposalsReceivedAsync(me);

            proposals.AddRange(await InternalGetProposalsSentAsync(me));

            var marriages = proposals.Select(x => x.Marriage).ToList();
            foreach(var i in marriages)
            {
                await marriageRepository.DeleteAsync(i);
            }

            await unitOfWork.CommitAsync();
        }

        public async Task DivorceAllMarriagesAsync(long me)
        {
            List<UserMarriedTo> marriageSet = await InternalGetMarriagesAsync(me);
            var marriages = marriageSet.Select(x => x.Marriage).ToList();
            foreach(var i in marriages)
            {
                await marriageRepository.DeleteAsync(i);
            }

            await unitOfWork.CommitAsync();
        }

        public async ValueTask DeclineProposalAsync(UserMarriedTo userMarriedTo)
        {
            await marriedToRepository.DeleteAsync(userMarriedTo);
            await unitOfWork.CommitAsync();
        }

        public async Task<bool> ExistsAsync(ulong id1, ulong id2)
            => await ExistsAsync((long)id1, (long)id2);

        public async Task<bool> ExistsAsync(long id1, long id2) => await GetEntryAsync(id1, id2) != null;

        public async Task<bool> ExistsMarriageAsync(long id1, long id2)
            => await GetMarriageAsync(id1, id2) != null;

        public async Task<List<UserMarriedTo>> GetProposalsSentAsync(long asker)
            => await InternalGetProposalsSentAsync(asker);

        public async Task<List<UserMarriedTo>> GetProposalsReceivedAsync(long userid)
            => await InternalGetProposalsReceivedAsync(userid);

        public async Task<UserMarriedTo> GetMarriageAsync(ulong receiver, ulong asker)
            => await GetMarriageAsync((long)receiver, (long)asker);

        public async Task<UserMarriedTo> GetMarriageAsync(long receiver, long asker)
        {
            UserMarriedTo m = await InternalGetMarriageAsync(receiver, asker);
            if(m == null)
            {
                m = await InternalGetMarriageAsync(asker, receiver);
            }

            return m;
        }

        public async Task<List<UserMarriedTo>> GetMarriagesAsync(long userid)
            => await InternalGetMarriagesAsync(userid);

        public async Task<UserMarriedTo> GetEntryAsync(ulong receiver, ulong asker)
            => await GetEntryAsync((long)receiver, (long)asker);

        public async ValueTask AcceptProposalAsync(Marriage marriage)
        {
            marriage.TimeOfMarriage = DateTime.Now;
            marriage.IsProposing = false;

            await marriageRepository.EditAsync(marriage);
            await unitOfWork.CommitAsync();
        }

        public async Task<UserMarriedTo> GetEntryAsync(long receiver, long asker)
        {
            UserMarriedTo marriedTo = await (await marriedToRepository.ListAsync())
                .AsQueryable()
                .Include(x => x.Marriage)
                .FirstOrDefaultAsync(x => x.AskerId == asker && x.ReceiverId == receiver
                                          || x.AskerId == receiver && x.ReceiverId == asker);
            return marriedTo;
        }

        public async Task ProposeAsync(long asker, long receiver)
        {
            Marriage m = await marriageRepository.AddAsync(new Marriage()
            {
                IsProposing = true,
                TimeOfProposal = DateTime.Now,
                TimeOfMarriage = DateTime.Now,
            });
            await unitOfWork.CommitAsync()
                .ConfigureAwait(false);

            await marriedToRepository.AddAsync(new UserMarriedTo()
            {
                MarriageId = m.MarriageId,
                ReceiverId = receiver,
                AskerId = asker
            });
            await unitOfWork.CommitAsync()
                .ConfigureAwait(false);
        }

        private async Task<List<UserMarriedTo>> InternalGetProposalsSentAsync(long asker)
        {
            var allInstances = await (await marriedToRepository.ListAsync())
                .AsQueryable()
                .Include(x => x.Marriage)
                .Where(x => x.AskerId == asker && x.Marriage.IsProposing)
                .ToListAsync();
            return allInstances;
        }

        private async Task<List<UserMarriedTo>> InternalGetProposalsReceivedAsync(long userid)
        {
            var allInstances = await (await marriedToRepository.ListAsync())
                .AsQueryable()
                .Include(x => x.Marriage)
                .Where(x => x.ReceiverId == userid && x.Marriage.IsProposing)
                .ToListAsync();
            if(!allInstances.Any())
            {
                throw new ProposalsEmptyException();
            }

            return allInstances;
        }

        private async Task<UserMarriedTo> InternalGetMarriageAsync(long receiver, long asker)
        {
            return await (await marriedToRepository.ListAsync())
                .AsQueryable()
                .Include(x => x.Marriage)
                .FirstOrDefaultAsync(x => x.ReceiverId == receiver
                                          && x.AskerId == asker
                                          && !x.Marriage.IsProposing);
        }

        private async Task<List<UserMarriedTo>> InternalGetMarriagesAsync(long userid)
        {
            var allInstances = await (await marriedToRepository.ListAsync())
                .AsQueryable()
                .Include(x => x.Marriage)
                .Where(x => (x.ReceiverId == userid || x.AskerId == userid) && !x.Marriage.IsProposing)
                .ToListAsync();
            return allInstances;
        }
    }
}