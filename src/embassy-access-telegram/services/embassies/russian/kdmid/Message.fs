[<RequireQualifiedAccess>]
module EA.Telegram.Services.Embassies.Russian.Kdmid.Message

open System
open Infrastructure.Domain
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain.Request
open EA.Core.Domain.Appointment
open EA.Core.Domain.Confirmation
open EA.Core.Domain.Notification
open EA.Core.Domain.ServiceNode
open EA.Core.Domain.ConfirmationState
open EA.Telegram.Router
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Russian.Clients.Domain.Kdmid

module Notification =
    open EA.Telegram.Router.Embassies.Russian
    open EA.Telegram.Router.Embassies.Russian.Kdmid.Post.Model

    open Infrastructure.Prelude

    let toSuccessfully (request: Request, msg: string) =
        fun chatId ->
            $"{msg} for '{request.Service.Name}' of '{request.Service.Embassy.ShortName}'."
            |> Text.create
            |> Message.createNew chatId

    let toUnsuccessfully (_: Request, error: Error') =
        fun chatId -> chatId |> Text.createError error

    let toHasAppointments (request: Request, appointments: Appointment Set) =
        fun chatId ->
            request.Service.Payload
            |> Payload.print
            |> Result.map (fun payload ->
                appointments
                |> Seq.map (fun appointment ->
                    let route =
                        {
                            RequestId = request.Id
                            AppointmentId = appointment.Id
                        }
                        |> Kdmid.Post.ConfirmAppointment
                        |> Post.Kdmid
                        |> Method.Post
                        |> Router.RussianEmbassy

                    route.Value, appointment.Value)
                |> fun data ->
                    (chatId, New)
                    |> ButtonsGroup.create {
                        Name =
                            $"Choose the appointment for subscription '{payload}' of service '{request.Service.Name}' at '{request.Service.Embassy.ShortName}'"
                        Columns = 1
                        Buttons =
                            data
                            |> Seq.map (fun (callback, name) -> callback |> CallbackData |> Button.create name)
                            |> Set.ofSeq
                    })

    let toHasConfirmations (request: Request, confirmations: Confirmation Set) =
        fun chatId ->
            request.Service.Payload
            |> Payload.print
            |> Result.map (fun payload ->
                $"The subscription '{payload}' for service '{request.Service.Name}' at '{request.Service.Embassy.ShortName}' was successfully applied.
                {Environment.NewLine}The response:{Environment.NewLine}
                '{confirmations |> Seq.map _.Description |> String.concat Environment.NewLine}'"
                |> Text.create
                |> Message.createNew chatId)

    let create request =
        fun chatId ->
            let errorFilter _ = true

            request
            |> Notification.tryCreate errorFilter
            |> Option.map (function
                | Empty(request, msg) -> chatId |> toSuccessfully (request, msg) |> Ok
                | Unsuccessfully(request, error) -> chatId |> toUnsuccessfully (request, error) |> Ok
                | HasAppointments(request, appointments) -> chatId |> toHasAppointments (request, appointments)
                | HasConfirmations(request, confirmations) -> chatId |> toHasConfirmations (request, confirmations))
            |> Option.defaultValue (
                $"I'm sorry, but I can't process your request for '{request.Service.Name}' of '{request.Service.Embassy.ShortName}'."
                |> Text.create
                |> Message.createNew chatId
                |> Ok
            )

    let spread notification =
        fun (deps: Kdmid.Notification.Dependencies) ->

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
                        chat.Id
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
                        chat.Id
                        |> toHasConfirmations (request, confirmations)
                        |> Result.map (fun x -> chat.Culture, x))
                    >> Result.choose
                )
                |> spreadMessages

module Instruction =

    open Infrastructure.Prelude
    open EA.Telegram.Router.Embassies.Russian
    open EA.Telegram.Router.Embassies.Russian.Kdmid.Post.Model

    let private toResponse instruction route =
        fun (chatId, messageId) ->

            let message = $"'{route}'{String.addLines 2}"

            instruction
            |> Option.map (fun instr -> message + $"Instruction:{String.addLines 2}{instr}")
            |> Option.defaultValue message
            |> fun message -> (chatId, messageId |> Replace) |> Text.create message

    let private toSubscribe embassyId (service: ServiceNode) confirmationState =
        fun (chatId, messageId) ->
            let request =
                {
                    ConfirmationState = confirmationState
                    ServiceId = service.Id
                    EmbassyId = embassyId
                    Payload = "<link>"
                }
                |> Kdmid.Post.Subscribe
                |> Post.Kdmid
                |> Method.Post
                |> Router.RussianEmbassy

            (chatId, messageId)
            |> toResponse service.Instruction request.Value
            |> Ok
            |> async.Return

    let toCheckAppointments embassyId (service: ServiceNode) =
        fun (deps: Kdmid.Dependencies) ->
            let request =
                {
                    ServiceId = service.Id
                    EmbassyId = embassyId
                    Payload = "<link>"
                }
                |> Kdmid.Post.CheckAppointments
                |> Post.Kdmid
                |> Method.Post
                |> Router.RussianEmbassy

            (deps.Chat.Id, deps.MessageId)
            |> toResponse service.Instruction request.Value
            |> Ok
            |> async.Return

    let toStandardSubscribe embassyId service =
        fun (deps: Kdmid.Dependencies) -> (deps.Chat.Id, deps.MessageId) |> toSubscribe embassyId service Disabled

    let toFirstAvailableAutoSubscribe embassyId service =
        fun (deps: Kdmid.Dependencies) ->
            (deps.Chat.Id, deps.MessageId)
            |> toSubscribe embassyId service FirstAvailable

    let toLastAvailableAutoSubscribe embassyId service =
        fun (deps: Kdmid.Dependencies) ->
            (deps.Chat.Id, deps.MessageId)
            |> toSubscribe embassyId service LastAvailable

    let toDateRangeAutoSubscribe embassyId service =
        fun (deps: Kdmid.Dependencies) ->
            (deps.Chat.Id, deps.MessageId)
            |> toSubscribe embassyId service (DateTimeRange(DateTime.MinValue, DateTime.MaxValue))
