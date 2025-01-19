module EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid.Query

open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Request
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Service.Kdmid
open EA.Embassies.Russian.Kdmid.Domain

let getSubscriptions (requests: EA.Core.Domain.Request.Request list) =
    fun (deps: Kdmid.Dependencies) ->
        requests
        |> Seq.map (fun request ->
            let route =
                request.Id
                |> Kdmid.Get.Appointments
                |> Get.Kdmid
                |> Request.Get
                |> RussianEmbassy

            request.Service.Payload
            |> Payload.toValue
            |> Result.map (fun payloadValue -> route.Value, payloadValue))
        |> Result.choose
        |> Result.map Map
        |> Result.map (fun data ->
            (deps.ChatId, Replace deps.MessageId)
            |> Buttons.create
                { Name = "Выберете подписку для проверки слота"
                  Columns = 1
                  Data = data })

let getAppointments requestId =
    fun (deps: Kdmid.Dependencies) ->
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
                |> Request.getService request
                |> ResultAsync.bind (fun result -> deps.ChatId |> Request.toResponse result))
