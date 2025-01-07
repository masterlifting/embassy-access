[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Producer.Core

open System
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Dependencies.Producer

let toAppointments (embassy: EmbassyNode, appointments: Set<Appointment>) =
    fun (deps: Core.Dependencies) ->
        embassy.Id
        |> deps.getEmbassyRequests
        |> ResultAsync.bindAsync (fun requests ->
            requests
            |> Seq.map _.Id
            |> deps.getEmbassyChats
            |> ResultAsync.map (
                Seq.map (fun chat ->
                    requests
                    |> Seq.map (fun request ->
                        request.Service.Payload
                        |> EA.Embassies.Russian.Kdmid.Domain.Payload.Payload.toValue
                        |> Result.map (fun payloadValue ->
                            appointments
                            |> Seq.map (fun appointment ->
                                let buttonKey =
                                    EA.Telegram.Endpoints.Consumer.Core
                                        .RussianEmbassy(
                                            Post(
                                                PostRequest.Kdmid(
                                                    { Confirmation = appointment.Id |> Manual |> Some
                                                      ServiceId = request.Service.Id
                                                      EmbassyId = embassy.Id
                                                      Payload = request.Service.Payload }
                                                )
                                            )
                                        )
                                        .Route

                                let buttonName = $"{appointment.Description} ({payloadValue})"

                                buttonKey, buttonName)))
                    |> Result.choose
                    |> Result.map Seq.concat
                    |> Result.map Map
                    |> Result.map (fun buttons ->
                        (chat.Id, New)
                        |> Buttons.create
                            { Name = $"Choose the appointment for '{embassy.ShortName}'"
                              Columns = 1
                              Data = buttons }))
            )
            |> ResultAsync.bind Result.choose)

let toConfirmations (requestId: RequestId, embassy: EmbassyNode, confirmations: Set<Confirmation>) =
    fun (deps: Core.Dependencies) ->
        deps.getSubscriptionChats requestId
        |> ResultAsync.map (
            List.map (fun chat ->
                confirmations
                |> Seq.map (fun confirmation -> $"'{embassy.ShortName}'. Confirmation: {confirmation.Description}")
                |> String.concat "\n"
                |> fun msg -> (chat.Id, New) |> Text.create msg)
        )

let toError (requestId: RequestId, error: Error') =
    fun (deps: Core.Dependencies) ->
        deps.getSubscriptionChats requestId
        |> ResultAsync.map (List.map (fun chat -> Web.Telegram.Producer.Text.createError error chat.Id))
