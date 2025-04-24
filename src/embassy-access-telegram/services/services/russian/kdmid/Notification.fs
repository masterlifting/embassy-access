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

        match request.ProcessState with
        | InProcess -> Ok() |> async.Return
        | Ready -> Ok() |> async.Return
        | Failed error ->
            // request.Payload
            // |> Payload.printError error
            // |> Option.map (Text.create >> Message.createNew chatId)
            $"The error message is not implemented yet: %s{error.Message}"
            |> NotImplemented
            |> Error
            |> async.Return
        | Completed _ ->
            match request.Payload.State with
            | NoAppointments -> Ok() |> async.Return
            | HasConfirmation(msg, _) ->
                // msg |> Text.create |> Message.createNew chatId |> Some
                $"Confirmation message is not implemented yet: %s{msg}"
                |> NotImplemented
                |> Error
                |> async.Return
            | HasAppointments appointments ->
                // appointments
                // |> Seq.map (fun a ->
                //     let route =
                //         Router.Services(
                //             Services.Method.Russian(
                //                 Services.Russian.Method.Kdmid(
                //                     Services.Russian.Kdmid.Method.Post(
                //                         Services.Russian.Kdmid.Post.ConfirmAppointment {
                //                             RequestId = request.Id
                //                             AppointmentId = a.Id
                //                         }
                //                     )
                //                 )
                //             )
                //         )
                //     route.Value, a.Value)
                // |> fun buttons ->
                //     ButtonsGroup.create {
                //         Name = "Choose the appointment"
                //         Columns = 1
                //         Buttons =
                //             buttons
                //             |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                //             |> Set.ofSeq
                //     }
                // |> Message.createNew chatId
                // |> Some
                $"Appointment message is not implemented yet: %A{appointments}"
                |> NotImplemented
                |> Error
                |> async.Return
