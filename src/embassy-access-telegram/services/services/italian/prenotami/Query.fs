module EA.Telegram.Services.Services.Italian.Prenotami.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Italian

let private (|CheckSlotsNow|SlotsAutoNotification|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; _; "0" ] -> CheckSlotsNow
    | [ _; _; _; _; "1" ] -> SlotsAutoNotification
    | _ -> ServiceNotFound

let private checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let private slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        match serviceId with
        | CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
        | SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
