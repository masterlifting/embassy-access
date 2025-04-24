[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Italian.Prenotami

open System.Threading
open EA.Core.Domain
open Infrastructure.Domain
open Web.Clients.Domain
open EA.Italian.Services.Domain.Prenotami
open EA.Telegram.Dependencies.Services.Italian

type Dependencies = {
    CT: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
} with

    static member create(deps: Italian.Dependencies) = {
        CT = deps.CT
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
    }

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        sendTranslatedMessagesRes: Chat -> Telegram.Producer.Message seq -> Async<Result<unit, Error'>>
    }
