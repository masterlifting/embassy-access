module EA.Telegram.Services.Embassies.RussianEmbassy

open System
open Infrastructure.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Router
open EA.Telegram.Endpoints.Consumer.Embassies.RussianEmbassy

module Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain
    open EA.Telegram.Endpoints.Consumer.Embassies.RussianEmbassy.Model.Kdmid

    module Notification =

        let toSuccessfullyResponse (request: Request.Request, msg: string) =
            fun chatId -> (chatId, New) |> Text.create msg

        let toUnsuccessfullyResponse (request: Request.Request, error: Error') =
            fun chatId -> chatId |> Text.createError error

        let toHasAppointmentsResponse (request: Request.Request, appointments: Appointment.Appointment Set) =
            fun chatId ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    appointments
                    |> Seq.map (fun appointment ->
                        let route =
                            RussianEmbassy(
                                Post(
                                    KdmidConfirmAppointment(
                                        { RequestId = request.Id
                                          AppointmentId = appointment.Id }
                                    )
                                )
                            )

                        route.Value, appointment.Description)
                    |> Map
                    |> fun buttons ->
                        (chatId, New)
                        |> Buttons.create
                            { Name = $"Choose the appointment for {request.Service.Embassy.ShortName}:{payloadValue}'"
                              Columns = 1
                              Data = buttons })

        let toHasConfirmationsResponse (request: Request.Request, confirmations: Confirmation.Confirmation Set) =
            fun chatId ->
                request.Service.Payload
                |> Payload.toValue
                |> Result.map (fun payloadValue ->
                    let resultMsg =
                        $"Confirmations for {request.Service.Embassy.ShortName}:{payloadValue} are found."

                    (chatId, New) |> Text.create resultMsg)
