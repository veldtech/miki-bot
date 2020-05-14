using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Framework;
using Miki.Patterns.Repositories;

namespace Miki.Services
{
    public class ConfigService : IAsyncDisposable
    {
        private readonly IUnitOfWork unit;
        private readonly IAsyncRepository<Config> configRepository;

        public ConfigService(IUnitOfWork unit)
        {
            this.unit = unit;
            configRepository = unit.GetRepository<Config>();
        }

        public async Task<Config> GetOrCreateAnyAsync(Guid? id)
        {
            if(id.HasValue)
            {
                return await GetOrInsertAsync(id.Value);
            }

            var value = await (await configRepository.ListAsync())
                .AsQueryable()
                .FirstOrDefaultAsync();
            if(value != null)
            {
                return value;
            }

            return await GetOrInsertAsync(new Guid());
        }

        public async Task<Config> GetOrInsertAsync(Guid id)
        {
            Guid configGuid = id;

            var value = await configRepository.GetAsync(configGuid);
            if(value == null)
            {
                return await InsertNewConfigAsync(configGuid);
            }
            return value;
        }

        public async Task<Config> InsertNewConfigAsync(Guid newId)
        {
            var configuration = new Config
            {
                Id = newId,
            };

            await configRepository.AddAsync(configuration);

            await unit.CommitAsync();

            return configuration;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return unit.DisposeAsync();
        }
    }
}
