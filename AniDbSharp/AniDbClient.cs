// Copyright © 2022 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AniDbSharp.Data;
using AniDbSharp.Exceptions;
using Polly;

namespace AniDbSharp
{
    public class AniDbClient : IAniDbClient
    {
        private const int ClientPort = 32569;
        private const int ServerPort = 9000;
        private const string Endpoint = "api.anidb.net";
        private const string SupportedApiVersion = "3";
        private static readonly List<string> UnauthWhitelist = new() { "PING", "ENCRYPT", "ENCODING", "AUTH", "VERSION" };
        private static readonly TimeSpan RequestCooldownTime = TimeSpan.FromSeconds(2);
        private static readonly AsyncPolicy RateLimitPolicy = Policy.RateLimitAsync(1, RequestCooldownTime);

        private readonly string username;
        private readonly string password;
        private readonly string clientName;
        private readonly int clientVersion;

        private bool isConnected;
        private bool isAuthenticated;
        private string? sessionToken;
        private UdpClient? udpClient;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public AniDbClient(string clientName, int clientVersion)
        {
            this.clientName = clientName;
            this.clientVersion = clientVersion;
        }

        /// <summary>
        /// Constructor with username and password
        /// </summary>
        /// <param name="clientName">Name of the client</param>
        /// <param name="clientVersion">Version of the client</param>
        /// <param name="username">Username to login with</param>
        /// <param name="password">Password to login with</param>
        public AniDbClient(string clientName, int clientVersion, string username, string password) : this(clientName, clientVersion)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            this.username = username;
            this.password = password;
        }

        public async Task<AniDbResponse> SendCommandAsync(string command, ParamBuilder? parameters = null)
        {
            if (!isConnected || udpClient == null)
            {
                throw new AniDbConnectionException("AniDbClient is not connected yet. Make sure to call .Connect() and that it successfully connects before trying to execute any commands.");
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentNullException(nameof(command));
            }

            command = command.ToUpperInvariant();

            if (!UnauthWhitelist.Contains(command) && !isAuthenticated)
            {
                throw new AniDbException($"Command \"{command}\" requires the client to be authenticated.");
            }

            if (parameters == null)
            {
                parameters = new ParamBuilder();
            }

            if (isAuthenticated)
            {
                parameters.Add("s", sessionToken);
            }

            byte[] bytes = Encoding.ASCII.GetBytes($"{command} {parameters}");

            if (bytes.Length > 1400)
            {
                throw new AniDbException("Request size greater than 1,400 bytes. Please ensure that you are formatting the request correctly.");
            }
            
            async Task<AniDbResponse> SendCommandInternal()
            {
                await udpClient.SendAsync(bytes, bytes.Length);

                var task = udpClient.ReceiveAsync();

                task.Wait(TimeSpan.FromSeconds(30));

                if (!task.IsCompleted)
                {
                    throw new AniDbConnectionRefusedException("AniDB refused to respond to request. Please wait a day and try again.");
                }

                var result = task.Result;

                string data = Encoding.ASCII.GetString(result.Buffer);

                string rawResponseCode = data.Substring(0, 3);

                CommandStatus responseStatus;

                if (int.TryParse(rawResponseCode, out int parsedResponseCode))
                {
                    if (!Enum.IsDefined(typeof(CommandStatus), parsedResponseCode))
                    {
                        throw new AniDbException($"Invalid response code \"{parsedResponseCode}\". Please check response code enum to ensure the value exists");
                    }

                    responseStatus = (CommandStatus)parsedResponseCode;
                }
                else
                {
                    throw new AniDbConnectionException($"Received invalid response from server: \"{data}\"");
                }

                string message = data.Substring(4);
                var body = new List<string>();

                if (message.Contains('\n'))
                {
                    string[] lines = message.Split('\n');

                    if (lines.Length > 0)
                    {
                        message = lines[0];
                    }

                    if (lines.Length > 1)
                    {
                        for (int idx = 1; idx < lines.Length; idx++)
                        {
                            if (!string.IsNullOrWhiteSpace(lines[idx]))
                            {
                                body.Add(lines[idx]);
                            }
                        }
                    }
                }

                var response = new AniDbResponse(responseStatus, message, body.ToArray());

                ThrowForGlobalErrors(response);

                return response;
            }

            return await RateLimitPolicy.ExecuteAsync(SendCommandInternal);
        }

        private Task<AniDbResponse> SendCommandIgnoreWarnings([NotNull] string command, ParamBuilder? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (parameters == null)
            {
                parameters = new ParamBuilder();
            }

            if (isAuthenticated)
            {
                parameters.Add("s", sessionToken);
            }

            throw new NotImplementedException();
        }

        private static void ThrowForGlobalErrors(AniDbResponse response)
        {
            if (response.Status == CommandStatus.Banned)
            {
                string reason = response.Message.Substring("555 Banned".Length).Trim();
                throw new AniDbException($"Banned: {reason}");
            }

            switch (response.Status)
            {
                case CommandStatus.IllegalInputOrAccessDenied:
                    throw new AniDbException("Illegal Input or Access Denied");
                case CommandStatus.UnknownCommand:
                    throw new AniDbException($"Unknown Command");
                case CommandStatus.InternalServerError:
                    throw new AniDbServerException("Internal Server Error");
                case CommandStatus.AniDbOutOfService:
                    throw new AniDbConnectionException("AniDB Out of Service - Try Again Later");
                case CommandStatus.ServerBusy:
                    throw new AniDbServerException("Server Busy - Try Again Later");
                case CommandStatus.Timeout:
                    throw new AniDbServerException("Timeout - Delay and Resubmit");
                case CommandStatus.LoginFirst:
                    throw new AniDbException("Login First");
                case CommandStatus.AccessDenied:
                    throw new AniDbException("Access Denied");
                case CommandStatus.InvalidSession:
                    throw new AniDbException("Invalid Session");
            }
        }

        /// <summary>
        /// Authenticate with the server and initialize a session
        /// </summary>
        /// <returns>Result of the auth request</returns>
        public async Task<AuthResult> AuthAsync()
        {
            var parameters = new ParamBuilder();

            parameters.Add("user", username);
            parameters.Add("pass", password);
            parameters.Add("protover", SupportedApiVersion);
            parameters.Add("client", clientName);
            parameters.Add("clientver", clientVersion);

            var response = await SendCommandAsync("AUTH", parameters);

            if (response.Status == CommandStatus.LoginAccepted || response.Status == CommandStatus.LoginAcceptedNewVersionAvailable)
            {
                string[] words = response.Message.Split(' ');

                if (words.Length == 0)
                {
                    throw new AniDbServerException($"Invalid response message for login \"{response.Message}\"");
                }

                sessionToken = words[0];
                isAuthenticated = true;

                return new AuthResult(true, response.Status == CommandStatus.LoginAcceptedNewVersionAvailable);
            }
            else if (response.Status == CommandStatus.ClientBanned)
            {
                if (response.Message.StartsWith("CLIENT BANNED - "))
                {
                    string reason = response.Message.Substring("CLIENT BANNED - ".Length).Trim();

                    throw new AniDbException($"Client Banned: {reason}");
                }
                else
                {
                    throw new AniDbServerException("Incorrectly formatted Client Banned response");
                }
            }

            switch (response.Status)
            {
                case CommandStatus.ClientVersionOutdated:
                    throw new AniDbException("Client version outdated. Please check for an updated client version.");
                default:
                    return new AuthResult(false);
            }
        }

        /// <summary>
        /// Search for file based on the file size and it's eD2k hash
        /// </summary>
        /// <param name="fileSize">Length of the file in bytes</param>
        /// <param name="ed2k">eD2k hash as byte array</param>
        /// <param name="fileMask">File mask for fields to retrieve</param>
        /// <param name="animeMask">Anime mask for fields to retrieve</param>
        /// <returns>File result for the search request</returns>
        public async Task<FileResult> SearchForFile(long fileSize, byte[] ed2k, FileMask fileMask, FileAnimeMask animeMask)
        {
            var parameters = new ParamBuilder();

            parameters.Add("size", fileSize);
            parameters.Add("ed2k", ed2k);
            parameters.Add("fmask", fileMask.GenerateBytes());
            parameters.Add("amask", animeMask.GenerateBytes());

            var response = await SendCommandAsync("FILE", parameters);

            FileResult result;

            if (response.Status == CommandStatus.File)
            {
                result = new FileResult(true, response);

                if (response.Body.Length < 1)
                {
                    throw new AniDbServerException("Invalid body length");
                }

                result.Parse(response.Body[0], fileMask, animeMask);
            }
            else if (response.Status == CommandStatus.NoSuchFile)
            {
                result = new FileResult(false, response);
            }
            else
            {
                throw new AniDbServerException($"Unknown FILE command response: {response.Status}");
            }

            return result;
        }

        /// <summary>
        /// Send logout command to the server
        /// </summary>
        /// <returns></returns>
        public async Task LogoutAsync()
        {
            await SendCommandAsync("LOGOUT");
        }

        /// <summary>
        /// Connect to AniDB server
        /// </summary>
        public void Connect()
        {
            try
            {
                udpClient = new UdpClient(ClientPort);
                udpClient.Connect(Endpoint, ServerPort);
                isConnected = true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                udpClient?.Dispose();
                udpClient = null;

                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (isConnected && isAuthenticated && udpClient != null)
            {
                var task = LogoutAsync();
                task.Wait();
            }
            udpClient?.Dispose();
            udpClient = null;
            isConnected = false;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (isConnected && isAuthenticated && udpClient != null)
            {
                await LogoutAsync();
            }
            udpClient?.Dispose();
            udpClient = null;
            isConnected = false;
        }
    }
}
