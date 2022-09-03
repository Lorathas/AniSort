using System.Text.Json;

namespace AniSort.Core;

public static class Constants
{
    public const string ApiClientName = "anidbapiclient";

    public const int ApiClientVersion = 1;
    
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
