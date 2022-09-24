namespace AniSort.Trackers.AniList

open System
open System.Net.Http
open System.Net.Http.Json
open AniSort.Sdk


module Tracker =
    [<Literal>]
    let OAuthAuthorizationEndpoint =
        "https://anilist.co/api/v2/oauth/authorize?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code"
        
    [<Literal>]
    let OAuthTokenEndpoint =
        "https://anilist.co/api/v2/oauth/token"

    [<Literal>]
    let ClientId = ""
    
    [<Literal>]
    let ClientSecret = ""
        
    [<Literal>]
    let OAuthRedirectEndpoint =
        ""

    type AniListTokenRequest =
        { grant_type: string
          client_id: string
          client_secret: string
          redirect_uri: string
          code: string }

    type AniListTracker(userCredentialsManager: IUserCredentialsManager) =
        member private this.userCredentialsManager =
            userCredentialsManager

        member val oAuthToken: string = null with get, set

        member private this.httpClient =
            new HttpClient()

        interface IDisposable with
            member this.Dispose() = this.httpClient.Dispose()

        interface IAnimeTracker<int> with
            member this.GetUserAnimeByIdAsync(id) = failwith "todo"
            member this.GetUserAnimeByNameAsync(name) = failwith "todo"
            member this.Name = "AniList"

            member this.LogIn() =
                task {
                    let! oAuthCode = this.userCredentialsManager.LoginWithOAuthAsync OAuthAuthorizationEndpoint

                    let body =
                        { grant_type = "authorization_code"
                          client_id = "client_id"
                          client_secret = "client_secret"
                          redirect_uri = "redirect_uri"
                          code = oAuthCode }

                    let! response = this.httpClient.PostAsync(OAuthTokenEndpoint, JsonContent.Create(body))
                    return response
                }

            member this.AuthenticatesWithOAuth = true
            member this.OAuthAuthorizationEndpoint = OAuthAuthorizationEndpoint
            
            member this.OAuthToken with set value =
                this.oAuthToken <- value

            member this.GetAccessToken(authorizationCode) =
                task {
                    let body =
                        { grant_type = "authorization_code"
                          client_id = "client_id"
                          client_secret = "client_secret"
                          redirect_uri = "redirect_uri"
                          code = authorizationCode }

                    let! response = this.httpClient.PostAsync(OAuthTokenEndpoint, JsonContent.Create(body))
                    return response
                }

            member this.ClientId = ClientId
            member this.ClientSecret = ClientSecret
