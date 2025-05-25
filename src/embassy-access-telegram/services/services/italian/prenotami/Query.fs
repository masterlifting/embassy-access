module EA.Telegram.Services.Services.Italian.Prenotami.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.Router

[<Literal>]
let private INPUT_LOGIN = "<login>"

[<Literal>]
let private INPUT_PASSWORD = "<password>"

let private createBaseRoute method =
    Router.Services(Services.Method.Italian(Method.Prenotami(method)))

let menu (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let startRequest =
                Prenotami.Method.Post(Prenotami.Post.StartManualRequest r.Id) |> createBaseRoute
            let info = Prenotami.Method.Get(Prenotami.Get.Info r.Id) |> createBaseRoute
            let delete =
                Prenotami.Method.Delete(Prenotami.Delete.Subscription(r.Id)) |> createBaseRoute

            ButtonsGroup.create {
                Name = r.Service.BuildName 1 "."
                Columns = 1
                Buttons =
                    [ "Info", info.Value; "Check now", startRequest.Value; "Delete", delete.Value ]
                    |> ButtonsGroup.createButtons
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let print (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

let private createPrenotamiInstruction chatId method =
    let route = Prenotami.Method.Post(method) |> createBaseRoute

    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace the {INPUT_LOGIN} and {INPUT_PASSWORD} with the login and password that you received from the prenotami website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        Prenotami.Post.SetManualRequest(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let private setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        Prenotami.Post.SetAutoNotifications(serviceId, embassyId, INPUT_LOGIN, INPUT_PASSWORD)
        |> createPrenotamiInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Prenotami.Method.Get(Prenotami.Get.Menu r.Id) |> createBaseRoute
                r.Service.BuildName 1 ".", route.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions to manage"
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
