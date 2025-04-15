module EA.Russian.Services.Midpass.Service

open EA.Core.Domain
open Infrastructure.Domain
open EA.Russian.Services.Domain.Midpass

let tryProcess (request: Request) =
    fun (deps: Client) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let tryProcessFirst (request: Request seq) =
    fun (deps: Client, notify) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
