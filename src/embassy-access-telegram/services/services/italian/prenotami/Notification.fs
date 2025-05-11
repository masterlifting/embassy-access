module EA.Telegram.Services.Services.Italian.Prenotami.Notification

open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services.Domain.Prenotami

let spread (request: Request<Payload>) =
    fun (deps: Prenotami.Notification.Dependencies) ->
        match request.ProcessState with
        | InProcess -> Ok() |> async.Return
        | Ready -> Ok() |> async.Return
        | Failed error ->
            let createMessage chatId =
                request.Payload
                |> Payload.printError error
                |> Option.map (Text.create >> Message.createNew chatId)

            deps.getRequestChats request
            |> ResultAsync.map (
                Seq.choose (fun chat -> createMessage chat.Id |> Option.map (fun message -> chat.Culture, message))
            )
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages
        | Completed _ ->
            match request.Payload.State with
            | NoAppointments -> Ok() |> async.Return
            | HasAppointments appointments ->
                let createMessage chatId =
                    appointments
                    |> Seq.map (fun a ->
                        let route =
                            Router.Services(
                                Services.Method.Italian(
                                    Services.Italian.Method.Prenotami(
                                        Services.Italian.Prenotami.Method.Post(
                                            Services.Italian.Prenotami.Post.ConfirmAppointment(request.Id, a.Id)
                                        )
                                    )
                                )
                            )
                        route.Value, a.Value)
                    |> fun buttons ->
                        ButtonsGroup.create {
                            Name = "Appointments available. Please select one."
                            Columns = 1
                            Buttons =
                                buttons
                                |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                                |> Set.ofSeq
                        }
                    |> Message.createNew chatId

                appointments
                |> deps.setRequestsAppointments request.Embassy.Id request.Service.Id
                |> ResultAsync.bindAsync (fun _ -> deps.getRequestChats request)
                |> ResultAsync.map (Seq.map (fun chat -> createMessage chat.Id |> fun message -> chat.Culture, message))
                |> ResultAsync.bindAsync deps.spreadTranslatedMessages
