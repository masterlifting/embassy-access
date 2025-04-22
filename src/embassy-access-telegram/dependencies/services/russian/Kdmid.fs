[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Kdmid

open System
open System.Threading
open Infrastructure.Domain
open EA.Core.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Russian.Services.Domain.Kdmid
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    Service: EA.Russian.Services.Domain.Kdmid.Dependencies
} with

    static member create(deps: Russian.Dependencies) =
        "Not implemented" |> NotImplemented |> Error

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        setAppointments: ServiceId -> Appointment Set -> Async<Result<Request<Payload> list, Error'>>
        translateMessages: Culture -> Message seq -> Async<Result<Message list, Error'>>
        sendMessages: Message seq -> Async<Result<unit, Error'>>
    }
