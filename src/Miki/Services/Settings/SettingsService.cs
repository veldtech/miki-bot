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

        public async ValueTask<Result<T>> GetAsync<T>(SettingType type, long entityId)
            where T : Enum
        {
            var setting = await settingsRepository.GetAsync(entityId, (int)type);
            if(setting == null)
            {
                return new Result<T>(
                    new EntityNullException<Setting>());
            }
            return (T)(object)setting.Value;
        } 

        public async ValueTask SetAsync<T>(SettingType type, long entityId, T value)
            where T : Enum
        {
            var result = await GetAsync<T>(type, entityId);
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
    }
}
