module EA.Telegram.Services.Services.Russian.Query

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Router

let private (|Kdmid|Midpass|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId |> parse with
    | Ok route ->
        match route with
        | Passport route ->
            match route with
            | Passport.Status -> Midpass
            | Passport.International ops -> Kdmid ops
        | Notary route ->
            match route with
            | Notary.PowerOfAttorney ops -> Kdmid ops
        | Citizenship route ->
            match route with
            | Citizenship.Renunciation ops -> Kdmid ops
    | Error error -> ServiceNotFound error

let getService embassyId serviceId forUser =
    fun (deps: Russian.Dependencies) ->
        match serviceId with
        | Kdmid ops ->
            deps
            |> Kdmid.Dependencies.create
            |> Kdmid.Query.getService ops serviceId embassyId forUser
        | Midpass ->
            deps
            |> Midpass.Dependencies.create
            |> Midpass.Query.getService serviceId embassyId forUser
        | ServiceNotFound _ ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
