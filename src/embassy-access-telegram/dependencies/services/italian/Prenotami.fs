[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Italian.Prenotami

open System.Threading
open EA.Core.Domain
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Italian.Services.Domain.Prenotami
open EA.Telegram.Dependencies.Services.Italian

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    Service: EA.Italian.Services.Domain.Prenotami.Dependencies
} with

    static member create(deps: Italian.Dependencies) =
        "Not implemented" |> NotImplemented |> Error

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        translateMessages: Culture -> Message seq -> Async<Result<Message list, Error'>>
        sendMessages: Message seq -> Async<Result<unit, Error'>>
    }
