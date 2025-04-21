module EA.Telegram.Services.Services.Russian.Kdmid.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Russian

let private (|CheckSlotsNow|SlotsAutoNotification|BookFirstSlot|BookLastSlot|BookFirstSlotInPeriod|ServiceNotFound|)
    (serviceId: ServiceId)
    =
    match serviceId.Value.Split() with
    | [ _; _; _; _; _; "0" ] -> CheckSlotsNow
    | [ _; _; _; _; _; "1" ] -> SlotsAutoNotification
    | [ _; _; _; _; _; "2"; "0" ] -> BookFirstSlot
    | [ _; _; _; _; _; "2"; "1" ] -> BookLastSlot
    | [ _; _; _; _; _; "2"; "2" ] -> BookFirstSlotInPeriod
    | _ -> ServiceNotFound

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
