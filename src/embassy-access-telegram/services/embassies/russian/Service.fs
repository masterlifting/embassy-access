module EA.Telegram.Services.Embassies.Russian.Service

open Infrastructure.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Request
open EA.Telegram.Endpoints.Embassies.Russian

module Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain

    module Notification =
        open EA.Telegram.Endpoints.Embassies.Russian.Kdmid.Post.Model

        let toSuccessfullyResponse (request: EA.Core.Domain.Request.Request, msg: string) =
            fun chatId ->
                $"{msg} for '{request.Service.Name}' of '{request.Service.Embassy.ShortName}'."
                |> Text.create
                |> Message.createNew chatId

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
                    |> fun data ->
                        (chatId, New)
                        |> ButtonsGroup.create
                            { Name =
                                $"Choose the appointment for subscription '{payloadValue}' of service '{request.Service.Name}' at '{request.Service.Embassy.ShortName}'"
                              Columns = 1
                              Buttons =
                                data
                                |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                                |> Set.ofSeq })

        let toHasConfirmationsResponse
            (request: EA.Core.Domain.Request.Request, confirmations: Confirmation.Confirmation Set)
            =
            fun chatId ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    $"The subscription '{payloadValue}' for service '{request.Service.Name}' at '{request.Service.Embassy.ShortName}' was successfully applied.\\n\\nThe result is: '{confirmations
                                                                                                                                                                                      |> Seq.map _.Description
                                                                                                                                                                                      |> String.concat System.Environment.NewLine}'"
                    |> Text.create
                    |> Message.createNew chatId)

    module Request =
        open EA.Embassies.Russian
        open EA.Embassies.Russian.Domain
        open EA.Telegram.Dependencies.Consumer.Embassies.Russian
        open EA.Embassies.Russian.Kdmid.Dependencies

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
                    $"I'm sorry, but I can't process your request for '{request.Service.Name}' of '{request.Service.Embassy.ShortName}'."
                    |> Text.create
                    |> Message.createNew chatId
                    |> Ok
                )

        let getService request =
            fun (deps: Kdmid.Dependencies) ->
                { Request = request
                  Dependencies = Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
                |> Kdmid
                |> API.Service.get
