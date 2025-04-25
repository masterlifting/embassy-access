module EA.Russian.Services.Domain.Midpass

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open EA.Core.DataAccess
open Web.Clients.Domain

// Use UTF-8 encoding for proper Cyrillic support
let private JsonOptions =
    Text.Json.JsonSerializerOptions(Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

type Payload = {
    Number: int
    State: string option
} with

    static member parse(payload: string) =
        match payload with
        | AP.IsInt id -> { Number = id; State = None } |> Ok
        | _ -> $"Midpass payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) =
        match payload.State with
        | Some state -> $"Number: '%i{payload.Number}', State: '%s{state}'"
        | None -> $"Number: '%i{payload.Number}'"

    static member serialize(payload: Payload) = payload |> Json.serialize' JsonOptions

    static member deserialize(payload: string) =
        payload |> Json.deserialize'<Payload> JsonOptions

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
}

type Dependencies = {
    RequestStorage: Request.Storage<Payload>
    CancellationToken: CancellationToken
}
