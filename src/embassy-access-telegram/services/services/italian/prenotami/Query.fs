module EA.Telegram.Services.Services.Italian.Prenotami.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Dependencies.Services.Italian

[<Literal>]
let private INPUT_LOGIN = "<login>"

[<Literal>]
let private INPUT_PASSWORD = "<password>"

let private (|CheckSlotsNow|SlotsAutoNotification|ServiceNotFound|) (serviceId: ServiceId) =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; _; "0" ] -> CheckSlotsNow
    | [ _; _; _; _; "1" ] -> SlotsAutoNotification
    | _ -> ServiceNotFound

let private createPrenotamiInstruction chatId method =
    let route =
        Router.Services(Services.Method.Italian(Method.Prenotami(Prenotami.Method.Post(method))))
    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace the {INPUT_LOGIN} and {INPUT_PASSWORD} with the login and password that you received from the prenotami website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        Prenotami.Post.CheckSlotsNow(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let private slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        Prenotami.Post.SlotsAutoNotification(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        match serviceId with
        | CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
        | SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
