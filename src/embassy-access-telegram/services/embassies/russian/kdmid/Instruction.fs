[<RequireQualifiedAccess>]
module EA.Telegram.Services.Embassies.Instruction

open System
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain.Service
open EA.Core.Domain.Confirmation
open EA.Telegram.Router
open EA.Telegram.Router.Embassies
open EA.Telegram.Router.Embassies.Post.Model

let private toResponse instruction route =
    fun (chatId, messageId) ->

        let message = $"'{route}'{String.addLines 2}"

        instruction
        |> Option.map (fun instr -> message + $"Instruction:{String.addLines 2}{instr}")
        |> Option.defaultValue message
        |> fun message -> (chatId, messageId |> Replace) |> Text.create message

let private toSubscribe embassyId (service: Service) confirmationState isBackgroundTask =
    fun (chatId, messageId) ->
        let request =
            {
                ServiceId = service.Id
                EmbassyId = embassyId
                IsBackground = isBackgroundTask
                ConfirmationState = confirmationState
                Payload = "<link>"
            }
            |> Post.Subscribe
            |> Method.Post
            |> Router.Embassies

        (chatId, messageId)
        |> toResponse service.Instruction request.Value
        |> Ok
        |> async.Return

let toCheckAppointments embassyId (service: Service) =
    fun (chatId, messageId) ->
        let request =
            {
                ServiceId = service.Id
                EmbassyId = embassyId
                Payload = "<link>"
            }
            |> Post.CheckAppointments
            |> Method.Post
            |> Router.Embassies

        (chatId, messageId)
        |> toResponse service.Instruction request.Value
        |> Ok
        |> async.Return

let toAutoNotifications embassyId service =
    fun (chatId, messageId) -> (chatId, messageId) |> toSubscribe embassyId service Disabled true

let toAutoFirstAvailableConfirmation embassyId service =
    fun (chatId, messageId) -> (chatId, messageId) |> toSubscribe embassyId service FirstAvailable true

let toAutoLastAvailableConfirmation embassyId service =
    fun (chatId, messageId) -> (chatId, messageId) |> toSubscribe embassyId service LastAvailable true

let toAutoDateRangeConfirmation embassyId service =
    fun (chatId, messageId) ->
        (chatId, messageId)
        |> toSubscribe embassyId service (DateTimeRange(DateTime.MinValue, DateTime.MaxValue)) true
