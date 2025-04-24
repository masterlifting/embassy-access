module EA.Telegram.Services.Services.Russian.Midpass.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Russian

let private (|PassportStatus|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value.Split() with
    | [ _; _; _; "1" ] -> PassportStatus
    | _ -> ServiceNotFound

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Midpass.Dependencies) ->
        $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
