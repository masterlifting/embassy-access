module EA.Telegram.Services.Services.Italian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Italian

let private (|Prenotami|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value.Split() with
    | [ _; _; _; _; "0" ]
    | [ _; _; _; _; "1" ] -> Prenotami
    | _ -> ServiceNotFound

let getService embassyId serviceId =
    fun (deps: Italian.Dependencies) ->
        match serviceId with
        | Prenotami ->
            Prenotami.Dependencies.create deps
            |> ResultAsync.wrap (Prenotami.Query.getService serviceId embassyId)
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
