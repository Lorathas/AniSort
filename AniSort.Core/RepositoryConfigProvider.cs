using System;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;

namespace AniSort.Core;

public class RepositoryConfigProvider : IConfigProvider
{
    private Config? config;

    private readonly ISettingRepository settingRepository;

    public RepositoryConfigProvider(ISettingRepository settingRepository)
    {
        this.settingRepository = settingRepository;
    }

    /// <inheritdoc />
    public Config? Config
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        get => config ??= settingRepository.GetSettings()?.Config ?? new Config();
        set => settingRepository.UpsertSettings(new Setting { Config = value ?? throw new ArgumentNullException(nameof(value)) });
    }
}
