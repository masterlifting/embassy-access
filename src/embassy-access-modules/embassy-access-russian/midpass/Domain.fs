module EA.Russian.Domain.Midpass

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Clients.Domain

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

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request<Payload> -> Async<Result<Request<Payload>, Error'>>
}
