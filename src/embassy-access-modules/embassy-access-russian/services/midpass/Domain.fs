module EA.Russian.Services.Domain.Midpass

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients.Domain

type Payload = {
    Value: int
} with

    static member create(payload: string) =
        match payload with
        | AP.IsInt id -> { Value = id } |> Ok
        | _ -> $"Midpass payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) = $"'ID:%i{payload.Value}'"

    static member serialize(payload: Payload) = Json.serialize payload

    static member deserialize(payload: string) = Json.deserialize<Payload> payload

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
}

type Dependencies = {
    RequestStorage: Request.Storage<Payload>
    CancellationToken: CancellationToken
}
