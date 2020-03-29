namespace Miki.Services.Settings
{
    using System;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework;
    using Miki.Functional;
    using Miki.Patterns.Repositories;

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
            return Result<T>.From(() => (T)(object)result.Unwrap().Value);
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
                return new Result<Setting>(new EntityNullException<Setting>());
            }
            return setting;
        }
    }
}
