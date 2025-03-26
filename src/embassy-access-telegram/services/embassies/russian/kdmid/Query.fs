module EA.Telegram.Services.Embassies.Russian.Kdmid.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Embassies.Russian
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Kdmid
open EA.Embassies.Russian.Kdmid.Domain

let private buildSubscriptionMenu (request: EA.Core.Domain.Request.Request) =
    let getRoute =
        request.Id
        |> Kdmid.Get.Appointments
        |> Get.Kdmid
        |> Method.Get
        |> Router.RussianEmbassy

    let deleteRoute =
        request.Id
        |> Kdmid.Delete.Subscription
        |> Delete.Kdmid
        |> Method.Delete
        |> Router.RussianEmbassy

    match request.Service.Id.Split() with
    | [ _; Constants.RUSSIAN_NODE_ID; _; _; "0" ] ->
        Map
            [ getRoute.Value, "Request available slots"
              deleteRoute.Value, "Delete subscription" ]
    | _ -> Map [ deleteRoute.Value, "Delete subscription" ]
    |> Seq.map (fun x -> x.Key |> CallbackData |> Button.create x.Value)
    |> Set.ofSeq

let getSubscriptions (requests: EA.Core.Domain.Request.Request list) =
    fun (deps: Kdmid.Dependencies) ->
        requests
        |> Seq.map (fun request ->
            let route =
                request.Id
                |> Kdmid.Get.SubscriptionsMenu
                |> Get.Kdmid
                |> Method.Get
                |> Router.RussianEmbassy

            request.Service.Payload
            |> Payload.toValue
            |> Result.map (fun payloadValue -> route.Value, payloadValue))
        |> Result.choose
        |> Result.map (fun data ->
            (deps.Chat.Id, Replace deps.MessageId)
            |> ButtonsGroup.create
                { Name = "Select the subscription"
                  Columns = 1
                  Buttons =
                    data
                    |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                    |> Set.ofSeq })

let getSubscriptionsMenu requestId =
    fun (deps: Kdmid.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bind (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.Chat.Id, New)
                |> Text.create
                    $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is still being processed."
                |> Ok
            | Ready
            | Failed _
            | Completed _ ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    (deps.Chat.Id, Replace deps.MessageId)
                    |> ButtonsGroup.create
                        { Name = $"What do you want to do with the subscription '{payloadValue}'?"
                          Columns = 1
                          Buttons = buildSubscriptionMenu request }))

let getAppointments requestId =
    fun (deps: Kdmid.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bindAsync (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.Chat.Id, New)
                |> Text.create
                    $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is already being processed."
                |> Ok
                |> async.Return
            | Ready
            | Failed _
            | Completed _ ->
                deps.getApi request
                |> ResultAsync.bind (fun result -> deps.Chat.Id |> Message.Notification.create result))
