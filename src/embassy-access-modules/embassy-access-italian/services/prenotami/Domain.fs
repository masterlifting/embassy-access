module EA.Italian.Services.Domain.Prenotami

open System
open System.Threading
open EA.Core.Domain
open EA.Core.DataAccess
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain

type Payload = {
    Login: string
    Password: string
} with

    static member create(payload: string) =
        match payload with
        | AP.IsString v ->
            let parts = v.Split ';'
            match parts.Length = 2 with
            | true ->
                let login = parts[0]
                let password = parts[1]
                { Login = login; Password = password } |> Ok
            | false -> $"Prenotami payload: '%s{payload}' is not supported." |> NotSupported |> Error
        | _ -> $"Prenotami payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) = $"Login: '%s{payload.Login}'"

type Client = {
    initHttpClient: Payload -> Result<Http.Client, Error'>
    updateRequest: Request -> Async<Result<Request, Error'>>
    getInitialPage: Http.Request -> Http.Client -> Async<Result<Http.Response<string>, Error'>>
}

type Dependencies = {
    RequestStorage: Request.RequestStorage
    CancellationToken: CancellationToken
}

module Constants =
    module ErrorCode =
        [<Literal>]
        let PAGE_HAS_ERROR = "PageHasError"

        [<Literal>]
        let INITIAL_PAGE_ERROR = "InitialPageError"
