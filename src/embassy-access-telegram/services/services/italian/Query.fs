module EA.Telegram.Services.Services.Italian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services.Router

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
    fun (deps: Italian.Dependencies) ->
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
