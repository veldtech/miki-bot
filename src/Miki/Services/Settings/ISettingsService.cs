using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Miki.Functional;

namespace Miki.Services.Settings
{
    /// <summary>
    /// User configurable settings service.
    /// </summary>
    public interface ISettingsService
    {
        ValueTask<Result<T>> GetAsync<T>(SettingType type, long entityId) where T : Enum;

        ValueTask SetAsync<T>(SettingType type, long entityId, T value) where T : Enum;
    }
}