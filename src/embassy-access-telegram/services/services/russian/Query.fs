module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Russian

let private (|Kdmid|Midpass|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; _; "0" ]
    | [ _; _; _; _; "1" ]
    | [ _; _; _; _; "2" ]
    | [ _; _; _; _; "2"; "0" ]
    | [ _; _; _; _; "2"; "1" ]
    | [ _; _; _; _; "2"; "2" ] -> Kdmid
    | [ _; _; "0"; "1" ] -> Midpass
    | _ -> ServiceNotFound

let getService embassyId serviceId =
    fun (deps: Russian.Dependencies) ->
        match serviceId with
        | Kdmid -> deps |> Kdmid.Dependencies.create |> Kdmid.Query.getService serviceId embassyId
        | Midpass ->
            deps
            |> Midpass.Dependencies.create
            |> Midpass.Query.getService serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
