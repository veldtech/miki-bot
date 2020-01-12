using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Framework;
using Miki.Patterns.Repositories;
using Miki.Services.Pasta.Exceptions;

namespace Miki.Services
{
    public interface IPaginated<T> 
        where T : class
    {
        int PageIndex { get; }
        IReadOnlyList<T> Items { get; }
          int PageCount { get; }
     
        Task<IPaginated<T>> GetNextPageAsync();
        Task<IPaginated<T>> GetPreviousPageAsync();

    }

    public struct PageInfo
    {
        public PageInfo(int index, int count)
        {
            pageCount = count;
            pageIndex = index;
        }

        public int pageIndex;
        public int pageCount;
    }

    public class PastaSearchResult : IPaginated<GlobalPasta>
    {
        private readonly PastaService service;
        private readonly PageInfo pageInfo;
        private readonly Expression<Func<GlobalPasta, bool>> whereFunc;

        public PastaSearchResult(
            PastaService service,
            PageInfo pageInfo,
            IEnumerable<GlobalPasta> items,
            Expression<Func<GlobalPasta, bool>> whereFunc) 
        {
            this.service = service;
            this.pageInfo = pageInfo;
            this.whereFunc = whereFunc;

            Items = items.ToList();
        }

        public int PageIndex => pageInfo.pageIndex;

        public IReadOnlyList<GlobalPasta> Items { get; private set; }

        public int PageCount => pageInfo.pageCount;

        public async Task<IPaginated<GlobalPasta>> GetNextPageAsync()
        {
            if(pageInfo.pageIndex == pageInfo.pageCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            return await service.SearchPastaAsync(
                whereFunc,
                pageInfo.pageIndex * Items.Count,
                Items.Count * pageInfo.pageIndex); 
        }

        public async Task<IPaginated<GlobalPasta>> GetPreviousPageAsync()
        {
            if(pageInfo.pageIndex == pageInfo.pageCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            return await service.SearchPastaAsync(
                whereFunc,
                pageInfo.pageIndex * Items.Count,
                (Items.Count * pageInfo.pageIndex) - Items.Count);
        }
    }

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

        public async ValueTask<IPaginated<GlobalPasta>> SearchPastaAsync(
            Expression<Func<GlobalPasta, bool>> where,
            int amount,
            int offset)
        {
            var query = (await repository.ListAsync())
                .AsQueryable();

            var result = await query
                .Where(where)
                .Skip(offset)
                .Take(amount)
                .ToListAsync();
            
            var count = await query.Where(where)
                .CountAsync();

            return new PastaSearchResult(
                this,
                new PageInfo(
                    1 + (int)Math.Ceiling((double)offset / amount),
                    (int)Math.Ceiling((double)count / amount)),
                result,
                where);
        }
    }
}
