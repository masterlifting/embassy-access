﻿module EA.Russian.Clients.Midpass.Service

open EA.Core.Domain
open Infrastructure.Domain
open EA.Russian.Clients.Domain.Midpass

let tryProcess (request: Request) =
    fun (deps: Dependencies) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED |> NotImplemented |> Error |> async.Return
        
let tryProcessFirst (request: Request seq) =
    fun (deps: Dependencies, notify) ->
        "Request for this service is not implemented yet. " + NOT_IMPLEMENTED |> NotImplemented |> Error |> async.Return
