using System.Threading.Tasks;

namespace AniSort.Sdk;

public interface IUserCredentialsManager
{
    Task<string> GetUsernameAsync();

    Task<string> GetPasswordAsync();

    Task<(string Username, string Password)> GetUserCredentialsAsync();

    Task<string> LoginWithOAuthAsync(string oAuthUrl);
}
