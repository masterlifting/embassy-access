﻿[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Domain

type Dependencies = {
    Chat: Chat
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Russian.Dependencies) = {
        Chat = deps.Chat
        MessageId = deps.MessageId
        CancellationToken = deps.CancellationToken
        translateMessageRes = deps.Culture.translateRes deps.Chat.Culture
        sendMessageRes = deps.sendMessageRes
    }
