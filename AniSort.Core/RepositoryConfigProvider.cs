using System;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;

namespace AniSort.Core;

public class RepositoryConfigProvider : IConfigProvider
{
    private Config? config;

    private readonly ISettingsRepository settingsRepository;

    public RepositoryConfigProvider(ISettingsRepository settingsRepository)
    {
        this.settingsRepository = settingsRepository;
    }

    /// <inheritdoc />
    public Config? Config
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        get => config ??= settingsRepository.GetSettings()?.Config ?? new Config();
        set => settingsRepository.UpsertSettings(new Setting { Config = value ?? throw new ArgumentNullException(nameof(value)) });
    }
}
