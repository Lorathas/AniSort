using System;
using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface ISettingRepository : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Get the current settings if they exist
    /// </summary>
    /// <returns></returns>
    Setting? GetSettings();

    /// <summary>
    /// Get the current settings if they exist
    /// </summary>
    /// <returns></returns>
    Task<Setting?> GetSettingsAsync();

    /// <summary>
    /// Upsert the settings
    /// </summary>
    /// <param name="setting">Settings to write</param>
    /// <returns></returns>
    Setting? UpsertSettings(Setting setting);

    /// <summary>
    /// Upsert the settings
    /// </summary>
    /// <param name="setting">Settings to write</param>
    /// <returns></returns>
    Task<Setting> UpsertSettingsAsync(Setting setting);
}
