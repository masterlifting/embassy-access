module EA.Telegram.Services.Embassies.Notification

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain.Request
open EA.Core.Domain.Appointment
open EA.Core.Domain.Confirmation
open EA.Core.Domain.Notification
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Router.Embassies.Post.Model
open EA.Telegram.Dependencies

let toSuccessfully (request: Request, msg: string) =
    fun chatId ->
        $"{msg} for '{request.Service.Name}' of '{request.Service.Embassy.Name}'."
        |> Text.create
        |> Message.createNew chatId

let toUnsuccessfully (_: Request, error: Error') =
    fun chatId -> chatId |> Text.createError error

let toHasAppointments (request: Request, appointments: Appointment Set) =
    fun (chatId, printPayload) ->
        request.Service.Payload
        |> printPayload
        |> Result.map (fun payload ->
            appointments
            |> Seq.map (fun appointment ->
                let route =
                    {
                        RequestId = request.Id
                        AppointmentId = appointment.Id
                    }
                    |> Post.ConfirmAppointment
                    |> Method.Post
                    |> Router.Embassies

                route.Value, appointment.Value)
            |> fun data ->
                (chatId, New)
                |> ButtonsGroup.create {
                    Name =
                        $"Choose the appointment for subscription '{payload}' of service '{request.Service.Name}' at '{request.Service.Embassy.Name}'"
                    Columns = 1
                    Buttons =
                        data
                        |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                        |> Set.ofSeq
                })

let toHasConfirmations (request: Request, confirmations: Confirmation Set) =
    fun (chatId, printPayload) ->
        request.Service.Payload
        |> printPayload
        |> Result.map (fun payload ->
            $"The subscription '{payload}' for service '{request.Service.Name}' at '{request.Service.Embassy.Name}' was successfully applied.
            {Environment.NewLine}The response:{Environment.NewLine}
            '{confirmations |> Seq.map _.Description |> String.concat Environment.NewLine}'"
            |> Text.create
            |> Message.createNew chatId)

let create request =
    fun (chatId, printPayload) ->
        let errorFilter _ = true

        request
        |> Notification.tryCreate errorFilter
        |> Option.map (function
            | Empty(request, msg) -> chatId |> toSuccessfully (request, msg) |> Ok
            | Unsuccessfully(request, error) -> chatId |> toUnsuccessfully (request, error) |> Ok
            | HasAppointments(request, appointments) ->
                (chatId, printPayload) |> toHasAppointments (request, appointments)
            | HasConfirmations(request, confirmations) ->
                (chatId, printPayload) |> toHasConfirmations (request, confirmations))
        |> Option.defaultValue (
            $"I'm sorry, but I can't process your request for '{request.Service.Name}' of '{request.Service.Embassy.Name}'."
            |> Text.create
            |> Message.createNew chatId
            |> Ok
        )

let spread notification =
    fun (deps: Notification.Dependencies) ->

        let translate (culture, messages) = deps.translateMessages culture messages

        let spreadMessages data =
            data
            |> ResultAsync.map (Seq.groupBy fst)
            |> ResultAsync.map (Seq.map (fun (culture, group) -> culture, group |> Seq.map snd |> List.ofSeq))
            |> ResultAsync.bindAsync (Seq.map translate >> Async.Parallel >> Async.map Result.choose)
            |> ResultAsync.map (Seq.collect id)
            |> ResultAsync.bindAsync deps.sendMessages

        match notification with
        | Empty _ -> () |> Ok |> async.Return
        | Unsuccessfully(request, error) ->
            request
            |> deps.getRequestChats
            |> ResultAsync.map (Seq.map (fun chat -> chat.Culture, chat.Id |> toUnsuccessfully (request, error)))
            |> spreadMessages
        | HasAppointments(request, appointments) ->
            appointments
            |> deps.setRequestAppointments request.Service.Id
            |> ResultAsync.bindAsync (fun _ -> request |> deps.getRequestChats)
            |> ResultAsync.bind (
                Seq.map (fun chat ->
                    (chat.Id, deps.printPayload)
                    |> toHasAppointments (request, appointments)
                    |> Result.map (fun x -> chat.Culture, x))
                >> Result.choose
            )
            |> spreadMessages
        | HasConfirmations(request, confirmations) ->
            request
            |> deps.getRequestChats
            |> ResultAsync.bind (
                Seq.map (fun chat ->
                    (chat.Id, deps.printPayload)
                    |> toHasConfirmations (request, confirmations)
                    |> Result.map (fun x -> chat.Culture, x))
                >> Result.choose
            )
            |> spreadMessages
