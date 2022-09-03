using System;
using System.Threading.Tasks;
using AniDbSharp.Data;

namespace AniDbSharp;

public interface IAniDbClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Send a command to AniDB
    /// </summary>
    /// <param name="command">Command to send</param>
    /// <param name="parameters">Parameters to send with the request</param>
    /// <returns></returns>
    Task<AniDbResponse> SendCommandAsync(string command, ParamBuilder? parameters = null);
    
    /// <summary>
    /// Authenticate with the server and initialize a session
    /// </summary>
    /// <returns>Result of the auth request</returns>
    Task<AuthResult> AuthAsync();

    /// <summary>
    /// Search for file based on the file size and it's eD2k hash
    /// </summary>
    /// <param name="fileSize">Length of the file in bytes</param>
    /// <param name="ed2k">eD2k hash as byte array</param>
    /// <param name="fileMask">File mask for fields to retrieve</param>
    /// <param name="animeMask">Anime mask for fields to retrieve</param>
    /// <returns>File result for the search request</returns>
    Task<FileResult> SearchForFile(long fileSize, byte[] ed2k, FileMask fileMask, FileAnimeMask animeMask);

    /// <summary>
    /// Initiate connection to AniDB
    /// </summary>
    void Connect();

    /// <summary>
    /// Logout from AniDB
    /// </summary>
    /// <returns>Task that returns when logout has finished</returns>
    Task LogoutAsync();
}