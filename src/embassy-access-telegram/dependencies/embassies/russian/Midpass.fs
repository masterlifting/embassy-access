[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Midpass

open System.Threading
open EA.Telegram.Dependencies
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Domain

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(deps: Request.Dependencies) = {
        ChatId = deps.ChatId
        MessageId = deps.MessageId
        CancellationToken = deps.CancellationToken
        translateMessageRes = deps.Culture.translateRes deps.
        sendMessageRes = deps.sendMessageRes
    }
