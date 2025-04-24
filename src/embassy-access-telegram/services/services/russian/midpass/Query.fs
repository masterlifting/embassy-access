module EA.Telegram.Services.Services.Russian.Midpass.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Russian

let private (|CheckPassportStatus|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; "1" ] -> CheckPassportStatus
    | _ -> ServiceNotFound

let private checkPassportStatus (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        match serviceId with
        | CheckPassportStatus -> deps |> checkPassportStatus serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
