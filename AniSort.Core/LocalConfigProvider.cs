namespace AniSort.Core;

public class LocalConfigProvider : IConfigProvider
{
    public LocalConfigProvider(Config config)
    {
        Config = config;
    }

    /// <inheritdoc />
    public Config? Config { get; set; }
}
