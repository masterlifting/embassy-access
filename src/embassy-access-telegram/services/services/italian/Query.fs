module EA.Telegram.Services.Services.Italian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Italian

let private (|Prenotami|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; _; "0" ]
    | [ _; _; _; _; "1" ] -> Prenotami
    | _ -> ServiceNotFound

let getService embassyId serviceId forUser =
    fun (deps: Italian.Dependencies) ->
        match serviceId with
        | Prenotami ->
            deps
            |> Prenotami.Dependencies.create
            |> Prenotami.Query.getService serviceId embassyId forUser
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
