using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories.EF;

public class SettingsRepository : ISettingsRepository
{
    private readonly AniSortContext context;
    
    public SettingsRepository(AniSortContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        context.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return context.DisposeAsync();
    }

    /// <inheritdoc />
    public Setting? GetSettings()
    {
        return context.Setting.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<Setting?> GetSettingsAsync()
    {
        return context.Setting.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public Setting? GetSettingsDetached()
    {
        var settings = GetSettings();

        context.Entry(settings).State = EntityState.Detached;

        return settings;
    }

    /// <inheritdoc />
    public async Task<Setting?> GetSettingsDetachedAsync()
    {
        
        var settings = await GetSettingsAsync();

        context.Entry(settings).State = EntityState.Detached;

        return settings;
    }

    /// <inheritdoc />
    public Setting UpsertSettings(Setting setting)
    {
        var entry = context.Entry(setting);
        
        entry.State =
            setting.Id == 0
                ? EntityState.Added
                : EntityState.Modified;

        if (entry.Entity.Id == 0)
        {
            entry.Entity.Id = 1;
        }

        context.SaveChanges();

        return entry.Entity;
    }

    /// <inheritdoc />
    public async Task<Setting> UpsertSettingsAsync(Setting setting)
    {
        if (setting.Id == 0)
        {
            setting.Id = 1;
        }
        
        var entry = context.Entry(setting);
        
        entry.State =
            await context.Setting.AnyAsync()
                ? EntityState.Modified
                : EntityState.Added;

        await context.SaveChangesAsync();

        var entity =  entry.Entity;

        entry.State = EntityState.Detached;
        
        return entity;
    }
}
