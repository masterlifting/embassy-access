module EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid.Instruction

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Embassies.Russian
open EA.Telegram.Endpoints.Request
open EA.Telegram.Endpoints.Embassies.Russian
open EA.Telegram.Endpoints.Embassies.Russian.Kdmid.Post.Model
open EA.Telegram.Dependencies.Consumer.Embassies.Russian

let private toResponse instruction route =
    fun (chatId, messageId) ->

        let message = $"{route}{String.addLines 2}"

        instruction
        |> Option.map (fun instr -> message + $"Инструкция:{String.addLines 2}{instr}")
        |> Option.defaultValue message
        |> fun message -> (chatId, messageId |> Replace) |> Text.create message

let private toSubscribe embassyId (service: ServiceNode) confirmation =
    fun (chatId, messageId) ->
        let request =
            { ConfirmationState = confirmation
              ServiceId = service.Id
              EmbassyId = embassyId
              Payload = "ссылку вставить сюда" }
            |> Kdmid.Post.Subscribe
            |> Post.Kdmid
            |> Request.Post
            |> RussianEmbassy

        (chatId, messageId)
        |> toResponse service.Instruction request.Value
        |> Ok
        |> async.Return

let toCheckAppointments embassyId (service: ServiceNode) =
    fun (deps: Kdmid.Dependencies) ->
        let request =
            { ServiceId = service.Id
              EmbassyId = embassyId
              Payload = "ссылку вставить сюда" }
            |> Kdmid.Post.CheckAppointments
            |> Post.Kdmid
            |> Request.Post
            |> RussianEmbassy

        (deps.Chat.Id, deps.MessageId)
        |> toResponse service.Instruction request.Value
        |> Ok
        |> async.Return

let toStandardSubscribe embassyId service =
    fun (deps: Kdmid.Dependencies) -> (deps.Chat.Id, deps.MessageId) |> toSubscribe embassyId service Disabled

let toFirstAvailableAutoSubscribe embassyId service =
    fun (deps: Kdmid.Dependencies) ->
        (deps.Chat.Id, deps.MessageId)
        |> toSubscribe embassyId service (ConfirmationState.Auto <| FirstAvailable)

let toLastAvailableAutoSubscribe embassyId service =
    fun (deps: Kdmid.Dependencies) ->
        (deps.Chat.Id, deps.MessageId)
        |> toSubscribe embassyId service (ConfirmationState.Auto <| LastAvailable)

let toDateRangeAutoSubscribe embassyId service =
    fun (deps: Kdmid.Dependencies) ->
        (deps.Chat.Id, deps.MessageId)
        |> toSubscribe embassyId service (ConfirmationState.Auto <| DateTimeRange(DateTime.MinValue, DateTime.MaxValue))
