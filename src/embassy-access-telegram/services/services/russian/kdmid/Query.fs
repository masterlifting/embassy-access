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

let private createBaseRoute method =
    Router.Services(Services.Method.Russian(Method.Kdmid(method)))

let menu (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let info = Kdmid.Method.Get(Kdmid.Get.Info r.Id) |> createBaseRoute
            let delete = Kdmid.Method.Delete(Kdmid.Delete.Subscription(r.Id)) |> createBaseRoute

            ButtonsGroup.create {
                Name = r.Service.FullName
                Columns = 1
                Buttons =
                    [ info.Value, "Info"; delete.Value, "Delete" ]
                    |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                    |> Set.ofSeq
            }
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let info (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

[<Literal>]
let private INPUT_LINK = "<link>"

let private createKdmidInstruction chatId method =
    let route = Kdmid.Method.Post(method) |> createBaseRoute

    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace the {INPUT_LINK} with the link that you received from the kdmid website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.CheckSlotsNow(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SlotsAutoNotification(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private bookFirstSlot (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookFirstSlot(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private bookFirstSlotInPeriod (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookFirstSlotInPeriod(serviceId, embassyId, DateTime.UtcNow, DateTime.UtcNow, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private bookLastSlot (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookLastSlot(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Kdmid.Method.Get(Kdmid.Get.Menu r.Id) |> createBaseRoute
                route.Value, r.Service.FullName)
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

let getService operation (serviceId: ServiceId) (embassyId: EmbassyId) forUser =
    fun (deps: Kdmid.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match operation with
            | Kdmid.Operation.CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
            | Kdmid.Operation.SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
            | Kdmid.Operation.AutoBookingFirstSlot -> deps |> bookFirstSlot serviceId embassyId
            | Kdmid.Operation.AutoBookingFirstSlotInPeriod -> deps |> bookFirstSlotInPeriod serviceId embassyId
            | Kdmid.Operation.AutoBookingLastSlot -> deps |> bookLastSlot serviceId embassyId
