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
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Italian.Dependencies) = {
        CT = deps.ct
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
        tryFindServiceNode = deps.tryFindServiceNode
        tryFindEmbassyNode = deps.tryFindEmbassyNode
        sendTranslatedMessageRes = deps.sendTranslatedMessageRes
    }

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        sendTranslatedMessagesRes: Chat -> Telegram.Producer.Message seq -> Async<Result<unit, Error'>>
    }
