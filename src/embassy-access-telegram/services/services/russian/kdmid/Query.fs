module EA.Telegram.Services.Services.Russian.Kdmid.Query

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian

[<Literal>]
let private PAST_LINK_HERE = "<past your link from the email instead of this>"

let private (|CheckSlotsNow|SlotsAutoNotification|BookFirstSlot|BookLastSlot|BookFirstSlotInPeriod|ServiceNotFound|)
    (serviceId: ServiceId)
    =
    match serviceId.Value |> Graph.NodeId.splitValues with
    | [ _; _; _; _; "0" ] -> CheckSlotsNow
    | [ _; _; _; _; "1" ] -> SlotsAutoNotification
    | [ _; _; _; _; "2"; "0" ] -> BookFirstSlot
    | [ _; _; _; _; "2"; "1" ] -> BookLastSlot
    | [ _; _; _; _; "2"; "2" ] -> BookFirstSlotInPeriod
    | _ -> ServiceNotFound

let private createKdmidInstruction chatId method =
    let route =
        Router.Services(Services.Method.Russian(Services.Russian.Method.Kdmid(Kdmid.Method.Post(method))))
    $"To use this service, please send the following command back to the bot: {String.addLines 2}'{route.Value}'{String.addLines 2}Without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.CheckSlotsNow(serviceId, embassyId, PAST_LINK_HERE)
        |> createKdmidInstruction deps.ChatId

let private slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.SlotsAutoNotification(serviceId, embassyId, PAST_LINK_HERE)
        |> createKdmidInstruction deps.ChatId

let private bookFirstSlot (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookFirstSlot(serviceId, embassyId, PAST_LINK_HERE)
        |> createKdmidInstruction deps.ChatId

let private bookLastSlot (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookLastSlot(serviceId, embassyId, PAST_LINK_HERE)
        |> createKdmidInstruction deps.ChatId

let private bookFirstSlotInPeriod (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        Kdmid.Post.BookFirstSlotInPeriod(serviceId, embassyId, PAST_LINK_HERE)
        |> createKdmidInstruction deps.ChatId

let getService (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        match serviceId with
        | CheckSlotsNow -> deps |> checkSlotsNow serviceId embassyId
        | SlotsAutoNotification -> deps |> slotsAutoNotification serviceId embassyId
        | BookFirstSlot -> deps |> bookFirstSlot serviceId embassyId
        | BookLastSlot -> deps |> bookLastSlot serviceId embassyId
        | BookFirstSlotInPeriod -> deps |> bookFirstSlotInPeriod serviceId embassyId
        | ServiceNotFound ->
            $"Service '%s{serviceId.ValueStr}' is not implemented. " + NOT_IMPLEMENTED
            |> NotImplemented
            |> Error
            |> async.Return
