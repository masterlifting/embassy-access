module EA.Italian.Services.Domain.Prenotami

open System
open System.Threading
open EA.Core.Domain
open EA.Core.DataAccess
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request -> Async<Result<Request, Error'>>
}

type Dependencies = {
    RequestStorage: Request.RequestStorage
    CancellationToken: CancellationToken
}

type Payload = {
    Value: int
} with

    static member create(payload: string) =
        match payload with
        | AP.IsInt id -> { Value = id } |> Ok
        | _ -> $"Prenotami payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) = $"'ID:%i{payload.Value}'"
