module EA.Russian.Clients.Domain.Midpass

open System
open EA.Core.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain

type Client = {
    initHttpClient: string -> Result<Http.Client, Error'>
    updateRequest: Request -> Async<Result<Request, Error'>>
}

type Dependencies = { Number: string }

type Payload = {
    Value: int
} with

    static member create(payload: string) =
        match payload with
        | AP.IsInt id -> { Value = id } |> Ok
        | _ -> $"Midpass payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) = $"'ID:%i{payload.Value}'"
