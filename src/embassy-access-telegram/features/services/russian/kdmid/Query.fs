module EA.Telegram.Features.Services.Russian.Kdmid.Query

open System
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Russian.Services.Domain.Kdmid
open EA.Russian.Services.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Router.Services.Russian.Kdmid
open EA.Telegram.Features.Dependencies.Services.Russian
open EA.Telegram.Router.Services

[<Literal>]
let private INPUT_LINK = "<link>"

let private buildRoute route =
    EA.Telegram.Router.Route.Services(Russian(Kdmid route))

let menu (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequest requestId)
        |> ResultAsync.map (fun r ->
            let start = Post(StartManualRequest r.Id) |> buildRoute
            let info = Kdmid.Get(Info r.Id) |> buildRoute
            let delete = Delete(Subscription r.Id) |> buildRoute

            ButtonsGroup.create {
                Name = r.Service.Value.BuildName 1 "."
                Columns = 1
                Buttons =
                    [ "Info", info.Value; "Check now", start.Value; "Delete", delete.Value ]
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
    let route = Post method |> buildRoute

    $"To use this service, please send the following command back to the bot:{String.addLines 2}'{route.Value}'"
    + $"{String.addLines 2}Replace {INPUT_LINK} with the link you received from the KDMID website after confirmation."
    + $"{String.addLines 1}Send the command without apostrophes, please."
    |> Text.create
    |> Message.createNew chatId
    |> Ok
    |> async.Return

let private setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        SetManualRequest(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        SetAutoNotifications(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingFirst (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        SetAutoBookingFirst(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingFirstInPeriod (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        SetAutoBookingFirstInPeriod(serviceId, embassyId, DateTime.UtcNow, DateTime.UtcNow, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private setAutoBookingLast (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        SetAutoBookingLast(serviceId, embassyId, INPUT_LINK)
        |> createKdmidInstruction deps.ChatId

let private getUserSubscriptions (serviceId: ServiceId) (embassyId: EmbassyId) =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.findRequests embassyId serviceId)
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun r ->
                let route = Kdmid.Get(Menu r.Id) |> buildRoute
                r.Service.Value.BuildName 1 ".", route.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Your subscriptions"
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
