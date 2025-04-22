module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Russian

let private (|Kdmid|Midpass|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value.Split() with
    | [ _; _; _; _; _; "0" ]
    | [ _; _; _; _; _; "1" ]
    | [ _; _; _; _; _; "2" ]
    | [ _; _; _; _; _; "2"; "0" ]
    | [ _; _; _; _; _; "2"; "1" ]
    | [ _; _; _; _; _; "2"; "2" ] -> Kdmid
    | [ _; _; "0"; "0"; "1" ] -> Midpass
    | _ -> ServiceNotFound

let getService embassyId serviceId =
    fun (deps: Russian.Dependencies) ->
        match serviceId with
        | Kdmid ->
            Kdmid.Dependencies.create deps
            |> ResultAsync.wrap (Kdmid.Query.getService serviceId embassyId)
        | Midpass ->
            Midpass.Dependencies.create deps
            |> ResultAsync.wrap (Midpass.Query.getService serviceId embassyId)
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
