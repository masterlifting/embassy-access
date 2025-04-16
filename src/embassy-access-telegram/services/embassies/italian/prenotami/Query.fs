module EA.Telegram.Services.Embassies.Italian.Prenotami.Query

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain.Constants
open EA.Core.Domain.Request
open EA.Core.Domain.ProcessState
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Services.Embassies
open EA.Telegram.Dependencies.Embassies.Italian
open EA.Italian.Services.Domain.Prenotami

let private buildSubscriptionMenu (request: Request) =
    let getRoute =
        request.Id
        |> Italian.Prenotami.Get.Appointments
        |> Italian.Get.Prenotami
        |> Italian.Method.Get
        |> Router.ItalianEmbassy

    let deleteRoute =
        request.Id |> Delete.Subscription |> Method.Delete |> Router.Embassies

    match request.Service.Id.Split() with
    | [ _; Embassies.RUS; _; _; "0" ] ->
        Map [
            getRoute.Value, "Request available slots"
            deleteRoute.Value, "Delete subscription"
        ]
    | _ -> Map [ deleteRoute.Value, "Delete subscription" ]
    |> Seq.map (fun x -> x.Key |> CallbackData |> Button.create x.Value)
    |> Set.ofSeq

let getSubscriptions (requests: Request list) =
    fun (deps: Prenotami.Dependencies) ->
        requests
        |> Seq.map (fun request ->
            let route =
                request.Id
                |> Italian.Prenotami.Get.SubscriptionsMenu
                |> Italian.Get.Prenotami
                |> Italian.Method.Get
                |> Router.ItalianEmbassy

            request.Service.Payload
            |> Credentials.create
            |> Result.map Credentials.print
            |> Result.map (fun payload -> route.Value, payload))
        |> Result.choose
        |> Result.map (fun data ->
            (deps.ChatId, Replace deps.MessageId)
            |> ButtonsGroup.create {
                Name = "Select the subscription"
                Columns = 1
                Buttons =
                    data
                    |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                    |> Set.ofSeq
            })

let getSubscriptionsMenu requestId =
    fun (deps: Prenotami.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bind (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.ChatId, New)
                |> Text.create
                    $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is still being processed."
                |> Ok
            | Ready
            | Failed _
            | Completed _ ->
                request.Service.Payload
                |> Credentials.create
                |> Result.map Credentials.print
                |> Result.map (fun payload ->
                    (deps.ChatId, Replace deps.MessageId)
                    |> ButtonsGroup.create {
                        Name = $"What do you want to do with the subscription '{payload}'?"
                        Columns = 1
                        Buttons = buildSubscriptionMenu request
                    }))

let getAppointments requestId =
    fun (deps: Prenotami.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bindAsync (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.ChatId, New)
                |> Text.create
                    $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is already being processed."
                |> Ok
                |> async.Return
            | Ready
            | Failed _
            | Completed _ ->
                let printPayload = Credentials.create >> Result.map Credentials.print

                deps.processRequest request
                |> ResultAsync.bind (fun result -> (deps.ChatId, printPayload) |> Notification.create result))
