module EA.Telegram.Services.Services.Russian.Kdmid.Query

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Kdmid

let private createBaseRoute method =
    Router.Services(Services.Method.Russian(Method.Kdmid(method)))

let menu (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let print = Kdmid.Method.Get(Kdmid.Get.Print r.Id) |> createBaseRoute
            let delete = Kdmid.Method.Delete(Kdmid.Delete.Subscription(r.Id)) |> createBaseRoute

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
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map Request.print<Payload>
        |> ResultAsync.map (Text.create >> Message.createNew deps.ChatId)

[<Literal>]
let private INPUT_LINK = "<link>"

let private (|CheckSlotsNow|SlotsAutoNotification|BookFirstSlot|BookLastSlot|BookFirstSlotInPeriod|OperationNotSupported|)
    (operations: string list)
    =
    match operations with
    | [ "0" ] -> CheckSlotsNow
    | [ "1" ] -> SlotsAutoNotification
    | [ "2"; "0" ] -> BookFirstSlot
    | [ "2"; "1" ] -> BookLastSlot
    | [ "2"; "2" ] -> BookFirstSlotInPeriod
    | _ -> OperationNotSupported

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

let private bookLastSlot (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookLastSlot(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private bookFirstSlotInPeriod (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookFirstSlotInPeriod(serviceId, embassyId, DateTime.UtcNow, DateTime.UtcNow, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Kdmid.Method.Get(Kdmid.Get.Menu r.Id) |> createBaseRoute
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

let getService operations (serviceId: ServiceId) (embassyId: EmbassyId) forUser =
    fun (deps: Kdmid.Dependencies) ->
        match forUser with
        | true -> deps |> getUserSubscriptions serviceId embassyId
        | false ->
            match operations with
            | CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
            | SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
            | BookFirstSlot -> deps |> bookFirstSlot serviceId embassyId
            | BookLastSlot -> deps |> bookLastSlot serviceId embassyId
            | BookFirstSlotInPeriod -> deps |> bookFirstSlotInPeriod serviceId embassyId
            | OperationNotSupported ->
                $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
                |> NotImplemented
                |> Error
                |> async.Return
