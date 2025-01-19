module EA.Telegram.Services.Embassies.Russian.Service

open System
open Infrastructure.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Request
open EA.Telegram.Endpoints.Consumer.Embassies.Russian

module Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain

    module Notification =
        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Kdmid.Post.Model

        let toSuccessfullyResponse (request: EA.Core.Domain.Request.Request, msg: string) =
            fun chatId -> (chatId, New) |> Text.create msg

        let toUnsuccessfullyResponse (request: EA.Core.Domain.Request.Request, error: Error') =
            fun chatId -> chatId |> Text.createError error

        let toHasAppointmentsResponse
            (request: EA.Core.Domain.Request.Request, appointments: Appointment.Appointment Set)
            =
            fun chatId ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    appointments
                    |> Seq.map (fun appointment ->
                        let route =
                            { RequestId = request.Id
                              AppointmentId = appointment.Id }
                            |> Kdmid.Post.ConfirmAppointment
                            |> Post.Kdmid
                            |> Request.Post
                            |> RussianEmbassy

                        route.Value, appointment.Description)
                    |> Map
                    |> fun buttons ->
                        (chatId, New)
                        |> Buttons.create
                            { Name = $"Choose the appointment for {request.Service.Embassy.ShortName}:{payloadValue}'"
                              Columns = 1
                              Data = buttons })

        let toHasConfirmationsResponse
            (request: EA.Core.Domain.Request.Request, confirmations: Confirmation.Confirmation Set)
            =
            fun chatId ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    let resultMsg =
                        $"Confirmations for {request.Service.Embassy.ShortName}:{payloadValue} are found."

                    (chatId, New) |> Text.create resultMsg)

    module Request =
        open EA.Embassies.Russian
        open EA.Embassies.Russian.Domain
        open EA.Telegram.Dependencies.Consumer.Embassies.Russian

        let toResponse request =
            fun chatId ->
                let errorFilter _ = true

                request
                |> Notification.tryCreate errorFilter
                |> Option.map (function
                    | Successfully(request, msg) -> chatId |> Notification.toSuccessfullyResponse (request, msg) |> Ok
                    | Unsuccessfully(request, error) ->
                        chatId |> Notification.toUnsuccessfullyResponse (request, error) |> Ok
                    | HasAppointments(request, appointments) ->
                        chatId |> Notification.toHasAppointmentsResponse (request, appointments)
                    | HasConfirmations(request, confirmations) ->
                        chatId |> Notification.toHasConfirmationsResponse (request, confirmations))
                |> Option.defaultValue (
                    (chatId, New)
                    |> Text.create $"Не валидный результат запроса {request.Id}."
                    |> Ok
                )

        let getService request =
            fun (deps: Kdmid.Dependencies) ->
                { Request = request
                  Dependencies = Kdmid.Dependencies.Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
                |> Kdmid
                |> API.Service.get
