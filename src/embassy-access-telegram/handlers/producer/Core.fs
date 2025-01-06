[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Producer.Core

open System
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Producer

let createAppointments (embassy: EmbassyNode, appointments: Set<Appointment>) =
    fun (deps: Core.Dependencies) ->
        embassy.Id
        |> deps.getEmbassyRequests
        |> ResultAsync.bindAsync (fun requests ->
            requests
            |> Seq.map _.Id
            |> deps.getEmbassyChats
            |> ResultAsync.map (
                Seq.map (fun chat ->
                    let buttons =
                        requests
                        |> Seq.collect (fun request ->
                            appointments
                            |> Seq.map (fun appointment ->
                                let route =
                                    EA.Telegram.Endpoints.Consumer.Core
                                        .RussianEmbassy(
                                            Post(
                                                PostRequest.Kdmid(
                                                    { Confirmation = Manual appointment.Id
                                                      ServiceId = request.Service.Id
                                                      EmbassyId = embassy.Id
                                                      Payload = request.Service.Payload }
                                                )
                                            )
                                        )
                                        .Route

                                route, appointment.Description))
                        |> Map

                    (chat.Id, New)
                    |> Buttons.create
                        { Name = $"Choose the appointment for '{embassy.ShortName}'"
                          Columns = 1
                          Data = buttons })
            ))

let createConfirmation (requestId: RequestId, embassy: EmbassyNode, confirmations: Set<Confirmation>) =
    fun (deps: Core.Dependencies) ->
        deps.initChatStorage ()
        |> ResultAsync.wrap (Chat.Query.findManyBySubscription requestId)
        |> ResultAsync.map (
            Seq.map (fun chat ->
                confirmations
                |> Seq.map (fun confirmation -> $"'{embassy.Name}'. Confirmation: {confirmation.Description}")
                |> String.concat "\n"
                |> fun msg -> (chat.Id, New) |> Text.create msg)
        )

let createError (requestId: RequestId, error: Error') =
    fun (deps: Core.Dependencies) ->
        deps.initChatStorage ()
        |> ResultAsync.wrap (Chat.Query.findManyBySubscription requestId)
        |> ResultAsync.map (Seq.map (fun chat -> Web.Telegram.Producer.Text.createError error chat.Id))
