using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services.Pasta.Exceptions;

namespace Miki.Services
{
    public class PastaService
    {
        private readonly IUnitOfWork unit;
        private readonly IAsyncRepository<GlobalPasta> repository;
        private readonly IAsyncRepository<PastaVote> voteRepository;

        public PastaService(IUnitOfWork unit)
        {
            this.unit = unit;
            this.repository = unit.GetRepository<GlobalPasta>();
            this.voteRepository = unit.GetRepository<PastaVote>();
        }

        public async ValueTask UpdatePastaAsync(string tag, string body, long userId)
        {
            var pasta = await repository.GetAsync(tag);
            if(pasta == null)
            {
                throw new PastaNotFoundException();
            }

            if(pasta.CreatorId != userId)
            {
                throw new ActionUnauthorizedException("edit");
            }

            pasta.Text = body;
            await repository.EditAsync(pasta);
            await unit.CommitAsync();
        }

        public async ValueTask DeletePastaAsync(string tag, long userId)
        {
            var pasta = await GetPastaAsync(tag);

            if(pasta.CreatorId != userId)
            {
                throw new ActionUnauthorizedException("delete");
            }
            await repository.DeleteAsync(pasta);

            var query = await (await voteRepository.ListAsync())
                .AsQueryable()
                .Where(x => x.Id == pasta.Id)
                .ToListAsync();
            foreach(var q in query)
            {
                await voteRepository.DeleteAsync(q);
            }
            await unit.CommitAsync();
        }

        public async ValueTask<GlobalPasta> CreatePastaAsync(string tag, string body, long createdBy)
        {
            GlobalPasta pasta = await repository.GetAsync(tag);
            if(pasta != null)
            {
                throw new DuplicatePastaException(pasta);
            }

            pasta = new GlobalPasta()
            {
                Id = tag,
                Text = body,
                CreatorId = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(pasta);
            await unit.CommitAsync();
            return pasta;
        }

        public async ValueTask<GlobalPasta> GetPastaOrDefaultAsync(string tag)
        {
            return await repository.GetAsync(tag);
        }

        public async ValueTask<GlobalPasta> GetPastaAsync(string tag)
        {
            var pasta = await GetPastaOrDefaultAsync(tag);
            if(pasta == null)
            {
                throw new PastaNotFoundException();
            }
            return pasta;
        }

        public async ValueTask UseAsync(GlobalPasta pasta)
        {
            pasta.TimesUsed++;
            await repository.EditAsync(pasta);
        }

        public async ValueTask<PastaVote> GetVoteAsync(string tag, long userId)
        {
            return await voteRepository.GetAsync(tag, userId);
        }

        public async ValueTask VoteAsync(PastaVote vote)
        {
            var pasta = await GetPastaAsync(vote.Id);  
            var currentVote = await GetVoteAsync(vote.Id, vote.UserId);
            
            if(currentVote == null)
            {
                await voteRepository.AddAsync(vote);
            } 
            else
            {
                await voteRepository.EditAsync(vote);
            }
            await unit.CommitAsync();

            pasta.Score = await GetScoreAsync(pasta.Id);
            await repository.EditAsync(pasta);
            await unit.CommitAsync();
        }

        public async ValueTask<int> GetScoreAsync(string tag)
        {
            var votes = await GetVotesAsync(tag);
            return votes.Upvotes - votes.Downvotes;
        }

        public async ValueTask<VoteCount> GetVotesAsync(string tag)
        {
            int up = await (await voteRepository.ListAsync())
                .AsQueryable()
                .Where(x => x.Id == tag && x.PositiveVote)
                .CountAsync();

            int down = await (await voteRepository.ListAsync())
                .AsQueryable()
                .Where(x => x.Id == tag && !x.PositiveVote)
                .CountAsync();

            return new VoteCount(up, down);
        }
    }
}
