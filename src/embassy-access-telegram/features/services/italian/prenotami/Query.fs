module EA.Telegram.Features.Services.Italian.Prenotami.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Italian.Services.Router
open EA.Italian.Services.Domain.Prenotami
open EA.Telegram.Router.Services
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Router.Services.Italian.Prenotami
open EA.Telegram.Features.Dependencies.Services.Italian

[<Literal>]
let private INPUT_LOGIN = "<login>"

[<Literal>]
let private INPUT_PASSWORD = "<password>"

let private buildRoute route =
    EA.Telegram.Router.Route.Services(Italian(Prenotami route))

let menu (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let start = Post(StartManualRequest r.Id) |> buildRoute
            let info = Get(Info r.Id) |> buildRoute
            let delete = Delete(Subscription r.Id) |> buildRoute

            ButtonsGroup.create {
                Name = r.Service.Value.BuildName 1 "."
                Columns = 1
                Buttons =
                    [ "Info", info.Value; "Check now", start.Value; "Delete", delete.Value ]
                    |> ButtonsGroup.createButtons
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let print (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

let private createPrenotamiInstruction chatId request =
    let route = Post request |> buildRoute

    $"To use this service, please send the following command back to the bot:{String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace {INPUT_LOGIN} and {INPUT_PASSWORD} with the login and password you received from the Prenotami website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        SetManualRequest(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let private setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        SetAutoNotifications(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Get(Menu r.Id) |> buildRoute
                r.Service.Value.BuildName 1 ".", route.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions"
                    Columns = 1
                    Buttons = buttons |> ButtonsGroup.createButtons
                }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let getService operation serviceId embassyId forUser =
    fun (deps: Prenotami.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match operation with
            | Prenotami.Operation.ManualRequest -> deps |> setManualRequest serviceId embassyId
            | Prenotami.Operation.AutoNotifications -> deps |> setAutoNotifications serviceId embassyId
