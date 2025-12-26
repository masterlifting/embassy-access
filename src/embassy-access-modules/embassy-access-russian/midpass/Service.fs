module EA.Russian.Midpass.Service

open EA.Core.Domain
open Infrastructure.Domain
open EA.Russian.Domain.Midpass

let tryProcess (request: Request<Payload>) =
    fun (deps: Client) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let tryProcessFirst (request: Request<Payload> seq) =
    fun (deps: Client, notify) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
