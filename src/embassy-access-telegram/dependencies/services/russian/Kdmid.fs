[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Kdmid

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain
open EA.Core.Domain
open EA.Russian.Services.Domain.Kdmid
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    CT: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
} with

    static member create(deps: Russian.Dependencies) = {
        CT = deps.CT
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
    }

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        setAppointments: ServiceId -> Appointment Set -> Async<Result<Request<Payload> list, Error'>>
        sendTranslatedMessagesRes: Chat -> Telegram.Producer.Message seq -> Async<Result<unit, Error'>>
    }
