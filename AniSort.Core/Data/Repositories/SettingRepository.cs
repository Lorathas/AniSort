using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace AniSort.Core.Data.Repositories;

public class SettingRepository : ISettingRepository
{
    private readonly AniSortContext context;
    
    public SettingRepository(AniSortContext context)
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
        var entry = context.Entry(setting);
        
        entry.State =
            setting.Id == 0
                ? EntityState.Added
                : EntityState.Modified;

        if (entry.Entity.Id == 0)
        {
            entry.Entity.Id = 1;
        }

        await context.SaveChangesAsync();

        return entry.Entity;
    }
}
