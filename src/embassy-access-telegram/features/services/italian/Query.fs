module EA.Telegram.Features.Services.Italian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Italian.Services.Router
open EA.Telegram.Features.Dependencies.Services.Italian

let private (|Prenotami|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId |> parse with
    | Ok route ->
        match route with
        | Visa route ->
            match route with
            | Visa.Tourism1 op -> Prenotami op
            | Visa.Tourism2 op -> Prenotami op
    | Error error -> ServiceNotFound error

let getService embassyId serviceId forUser =
    fun (deps: Root.Dependencies) ->
        match serviceId with
        | Prenotami op ->
            deps
            |> Prenotami.Dependencies.create
            |> Prenotami.Query.getService op serviceId embassyId forUser
        | ServiceNotFound _ ->
            $"Service '%s{serviceId.Value}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
