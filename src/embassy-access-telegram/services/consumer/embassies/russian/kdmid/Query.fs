module EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid.Query

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Router
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Request
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Embassies.Russian
open EA.Embassies.Russian.Kdmid.Dependencies
open EA.Embassies.Russian.Kdmid.Domain
open EA.Embassies.Russian.Domain.Service
open EA.Telegram.Services.Embassies.Russian.Service.Kdmid

let private toCheckAppointments (requests: EA.Core.Domain.Request.Request list) =
    fun (deps: RussianEmbassy.Dependencies) ->
        requests
        |> Seq.map (fun request ->
            let route = RussianEmbassy(Get(Get.Request.KdmidCheckAppointments request.Id))

            request.Service.Payload
            |> Payload.toValue
            |> Result.map (fun payloadValue -> route.Value, payloadValue))
        |> Result.choose
        |> Result.map Map
        |> Result.map (fun data ->
            (deps.ChatId, Replace deps.MessageId)
            |> Buttons.create
                { Name = "Выберите услугу для проверки записи"
                  Columns = 1
                  Data = data })
        
let private toResponse (request: EA.Core.Domain.Request.Request) =
    fun chatId ->
        let errorFilter _ = true

        request
        |> Notification.tryCreate errorFilter
        |> Option.map (function
            | Successfully(request, msg) -> chatId |> Notification.toSuccessfullyResponse (request, msg) |> Ok
            | Unsuccessfully(request, error) -> chatId |> Notification.toUnsuccessfullyResponse (request, error) |> Ok
            | HasAppointments(request, appointments) ->
                chatId |> Notification.toHasAppointmentsResponse (request, appointments)
            | HasConfirmations(request, confirmations) ->
                chatId |> Notification.toHasConfirmationsResponse (request, confirmations))
        |> Option.defaultValue (
            (chatId, New)
            |> Text.create $"Не валидный результат запроса {request.Id}."
            |> Ok
        )

let private getService request =
    fun (deps: RussianEmbassy.Dependencies) ->
        { Request = request
          Dependencies = Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
        |> Kdmid
        |> API.Service.get

let checkAppointments requestId =
    fun (deps: RussianEmbassy.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bindAsync (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.ChatId, New)
                |> Text.create
                    $"Запрос на услугу '{request.Service.Name}' для посольства '{request.Service.Embassy.Name}' уже в обработке."
                |> Ok
                |> async.Return
            | _ ->
                deps
                |> getService request
                |> ResultAsync.bind (fun result -> deps.ChatId |> toResponse result))

let getData embassyId (service: ServiceNode) =
    fun (deps: RussianEmbassy.Dependencies) ->
        deps.getChatRequests ()
        |> ResultAsync.map (
            List.filter (fun request ->
                request.Service.Id = service.Id && request.Service.Embassy.Id = embassyId)
        )
        |> ResultAsync.bind (fun requests ->
            match service.Id.Value |> Graph.split with
            | [ _; "RU"; _; _; "0" ] -> deps |> toCheckAppointments requests
            | _ -> service.ShortName |> NotSupported |> Error)