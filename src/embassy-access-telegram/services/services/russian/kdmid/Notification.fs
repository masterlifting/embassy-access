module EA.Telegram.Services.Services.Russian.Kdmid.Notification

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian.Kdmid
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Kdmid

let spread (request: Request<Payload>) =
    fun (deps: Kdmid.Notification.Dependencies) ->

        let createMessages chatId =
            match request.ProcessState with
            | InProcess -> None
            | Ready -> None
            | Failed error ->
                request.Payload
                |> Payload.printError error
                |> Option.map (Text.create >> Message.createNew chatId)
                |> Option.map Seq.singleton
            | Completed _ ->
                match request.Payload.Appointments.IsEmpty with
                | true -> None
                | false ->
                    let withConfirmations, withoutConfirmations =
                        request.Payload.Appointments |> Set.partition _.Confirmation.IsSome

                    let confirmationMessages =
                        withConfirmations
                        |> Seq.choose _.Confirmation
                        |> Seq.map (fun c -> c |> Text.create |> Message.createNew chatId)

                    let appointmentButtons =
                        withoutConfirmations
                        |> Seq.map (fun a ->
                            let route =
                                Router.Services(
                                    Services.Method.Russian(
                                        Services.Russian.Method.Kdmid(
                                            Method.Post(
                                                Post.ConfirmAppointment {
                                                    RequestId = request.Id
                                                    AppointmentId = a.Id
                                                }
                                            )
                                        )
                                    )
                                )
                            route.Value, a.Value)
                        |> fun buttons ->
                            ButtonsGroup.create {
                                Name = "Choose the appointment"
                                Columns = 1
                                Buttons =
                                    buttons
                                    |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                                    |> Set.ofSeq
                            }
                        |> Message.createNew chatId

                    (confirmationMessages |> Seq.append [ appointmentButtons ]) |> Some

        createMessages
