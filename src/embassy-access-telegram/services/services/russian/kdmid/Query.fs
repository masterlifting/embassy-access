module EA.Telegram.Services.Services.Russian.Kdmid.Query

open System
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Kdmid
open EA.Russian.Services.Router

[<Literal>]
let private INPUT_LINK = "<link>"

let private createBaseRoute method =
    Router.Services(Services.Method.Russian(Method.Kdmid(method)))

let menu (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let startRequest =
                Kdmid.Method.Post(Kdmid.Post.StartManualRequest r.Id) |> createBaseRoute
            let info = Kdmid.Method.Get(Kdmid.Get.Info r.Id) |> createBaseRoute
            let delete = Kdmid.Method.Delete(Kdmid.Delete.Subscription(r.Id)) |> createBaseRoute

            ButtonsGroup.create {
                Name = r.Service.BuildName 1 "."
                Columns = 1
                Buttons =
                    [ "Info", info.Value; "Check now", startRequest.Value; "Delete", delete.Value ]
                    |> ButtonsGroup.createButtons
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let info (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

let private createKdmidInstruction chatId method =
    let route = Kdmid.Method.Post(method) |> createBaseRoute

    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace the {INPUT_LINK} with the link that you received from the kdmid website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SetManualRequest(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SetAutoNotifications(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingFirst (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SetAutoBookingFirst(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingFirstInPeriod (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SetAutoBookingFirstInPeriod(serviceId, embassyId, DateTime.UtcNow, DateTime.UtcNow, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingLast (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SetAutoBookingLast(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Kdmid.Method.Get(Kdmid.Get.Menu r.Id) |> createBaseRoute
                r.Service.BuildName 1 ".", route.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions to manage"
                    Columns = 1
                    Buttons = buttons |> ButtonsGroup.createButtons
                }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let getService operation (serviceId: ServiceId) (embassyId: EmbassyId) forUser =
    fun (deps: Kdmid.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match operation with
            | Kdmid.Operation.ManualRequest -> deps |> setManualRequest serviceId embassyId
            | Kdmid.Operation.AutoNotifications -> deps |> setAutoNotifications serviceId embassyId
            | Kdmid.Operation.AutoBookingFirst -> deps |> setAutoBookingFirst serviceId embassyId
            | Kdmid.Operation.AutoBookingFirstInPeriod -> deps |> setAutoBookingFirstInPeriod serviceId embassyId
            | Kdmid.Operation.AutoBookingLast -> deps |> setAutoBookingLast serviceId embassyId
