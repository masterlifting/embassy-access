module EA.Telegram.Services.Services.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies

let getService (embassyId: EmbassyId) =
    fun (deps: Services.Dependencies) ->
            match
                embassyId.Value
                |> Graph.NodeId.split
                |> Seq.skip 1
                |> Seq.tryHead
                |> Option.map _.Value
            with
            | Some countryId ->
                match countryId with
                | Embassies.RUS ->
                    deps.initRussianDeps ()
                    |> ResultAsync.wrap (Russian.Query.getServices embassyId)
                | Embassies.ITA ->
                    deps.initItalianDeps ()
                    |> ResultAsync.wrap (Italian.Query.getServices embassyId)
                | _ ->
                    $"Service for '%s{countryId}' is not implemented. " + NOT_IMPLEMENTED
                    |> NotImplemented
                    |> Error
                    |> async.Return
            | None ->
                $"Service for '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return

let getUserService (embassyId: EmbassyId) =
    fun (deps: Services.Dependencies) ->
            match
                embassyId.Value
                |> Graph.NodeId.split
                |> Seq.skip 1
                |> Seq.tryHead
                |> Option.map _.Value
            with
            | Some countryId ->
                match countryId with
                | Embassies.RUS ->
                    deps.initRussianDeps ()
                    |> ResultAsync.wrap (Russian.Query.getServices embassyId)
                | Embassies.ITA ->
                    deps.initItalianDeps ()
                    |> ResultAsync.wrap (Italian.Query.getServices embassyId)
                | _ ->
                    $"Service for '%s{countryId}' is not implemented. " + NOT_IMPLEMENTED
                    |> NotImplemented
                    |> Error
                    |> async.Return
            | None ->
                $"Service for '%s{embassyId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
