module EA.Telegram.Services.Services.Italian.Prenotami.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services.Domain.Prenotami

let private createBaseRoute method =
    Router.Services(Services.Method.Italian(Method.Prenotami(method)))

let menu (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let print = Prenotami.Method.Get(Prenotami.Get.Print r.Id) |> createBaseRoute
            let delete =
                Prenotami.Method.Delete(Prenotami.Delete.Subscription(r.Id)) |> createBaseRoute

            ButtonsGroup.create {
                Name = "Manage your subscription"
                Columns = 1
                Buttons =
                    [ print.Value, "Print"; delete.Value, "Delete" ]
                    |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                    |> Set.ofSeq
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let print (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

[<Literal>]
let private INPUT_LOGIN = "<login>"

[<Literal>]
let private INPUT_PASSWORD = "<password>"

let private (|CheckSlotsNow|SlotsAutoNotification|OperationNotSupported|) (operation: string) =
    match operation with
    | "0" -> CheckSlotsNow
    | "1" -> SlotsAutoNotification
    | _ -> OperationNotSupported

let private createPrenotamiInstruction chatId method =
    let route = Prenotami.Method.Post(method) |> createBaseRoute

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

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Prenotami.Method.Get(Prenotami.Get.Menu r.Id) |> createBaseRoute
                route.Value, r.Payload.Credentials |> Credentials.print)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions to manage"
                    Columns = 1
                    Buttons =
                        buttons
                        |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                        |> Set.ofSeq
                }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let getService operation serviceId embassyId forUser =
    fun (deps: Prenotami.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match operation with
            | CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
            | SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
            | OperationNotSupported ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
