using System;
using System.Threading.Tasks;
using AniSort.Sdk.Data;

namespace AniSort.Sdk;

public interface IAnimeTracker<TId>
{
    string Name { get; }
    
    bool AuthenticatesWithOAuth { get; }
    
    string OAuthAuthorizationEndpoint { get; }
    
    string OAuthToken { set; }
    
    string ClientId { get; }
    
    string ClientSecret { get; }
    
    Task<UserAnime<TId>> GetUserAnimeByNameAsync(string name);

    Task<UserAnime<TId>> GetUserAnimeByIdAsync(TId id);

    Task GetAccessToken(string authorizationCode);

    Task LogIn();
}
