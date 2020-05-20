using System;
using System.Threading.Tasks;
using Miki.Bot.Models;
using Miki.Bot.Models.Exceptions;
using Miki.Framework;
using Miki.Functional;
using Miki.Patterns.Repositories;

namespace Miki.Services.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly IUnitOfWork unit;
        private readonly IAsyncRepository<Setting> settingsRepository;

        public SettingsService(IUnitOfWork unit)
        {
            this.unit = unit;
            this.settingsRepository = unit.GetRepository<Setting>();
        }

        public async ValueTask<Result<T>> GetAsync<T>(SettingType type, long entityId)
            where T : Enum
        {
            var result = await InternalGetAsync((int)type, entityId);
            if(result.IsValid)
            {
                return (T)(object)result.Unwrap().Value;
            }
            return result.UnwrapException();
        } 

        public async ValueTask SetAsync<T>(SettingType type, long entityId, T value)
            where T : Enum
        {
            var result = await InternalGetAsync((int)type, entityId);
            var setting = new Setting
            {
                EntityId = entityId,
                SettingId = (int)type,
                Value = (int)(object)value
            };

            if(result.IsValid)
            {
                await settingsRepository.EditAsync(setting);
            }
            else
            {
                await settingsRepository.AddAsync(setting);
            }

            await unit.CommitAsync();
        }

        private async ValueTask<Result<Setting>> InternalGetAsync(int type, long entityId)
        {
            var setting = await settingsRepository.GetAsync(entityId, type);
            if(setting == null)
            {
                return new EntityNullException<Setting>();
            }
            return setting;
        }
    }
}
