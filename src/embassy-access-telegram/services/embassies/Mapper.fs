[<RequireQualifiedAccess>]
module EA.Telegram.Services.Embassies.Mapper

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Embassies
open Infrastructure.Prelude

let mapService (serviceId: Graph.NodeId) =
    fun (deps: Embassies.Dependencies) ->
        let requestIdParts = serviceId.Split() |> List.skip 1

        match requestIdParts |> List.tryHead with
        | Some Embassies.RUS ->
            match requestIdParts |> List.skip 2 with
            | [ "0"; "0"; "1" ] ->
                deps.Russian.Midpass
                |> EA.Russian.Clients.Midpass.Client.init
                |> ResultAsync.wrap EA.Russian.Clients.Midpass.Service.tryProcess
            | _ ->
                deps.Russian.Kdmid
                |> EA.Russian.Clients.Kdmid.Client.init
                |> ResultAsync.wrap EA.Russian.Clients.Kdmid.Service.tryProcess
        | Some Embassies.ITA ->
            "The Italian embassy is not implemented yet. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
        | _ ->
            $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
