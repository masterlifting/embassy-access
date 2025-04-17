[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Italian.Prenotami

open System.Threading
open EA.Core.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.Prenotami

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    getRequest: RequestId -> Async<Result<Payload, Error'>>
    processRequest: Payload -> Async<Result<Payload, Error'>>
    Service: EA.Italian.Services.Domain.Prenotami.Dependencies
} with

    static member create(deps: Italian.Dependencies) =
        let getRequest requestId =
            deps.RequestStorage
            |> Request.Query.tryFindById requestId
            |> ResultAsync.bind (function
                | None ->
                    $"Request '%s{requestId.ValueStr}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                    |> NotFound
                    |> Error
                | Some request -> request |> Ok)
        let processRequest request =
            {
                CancellationToken = deps.CancellationToken
                RequestStorage = deps.RequestStorage
            }
            |> Client.init
            |> ResultAsync.wrap (Service.tryProcess request)
        {
            ChatId = deps.Chat.Id
            MessageId = deps.MessageId
            CancellationToken = deps.CancellationToken
            translateMessageRes = deps.Culture.translateRes deps.Chat.Culture
            sendMessageRes = deps.sendMessageRes
            getRequest = getRequest
            processRequest = processRequest
            Service = {
                CancellationToken = deps.CancellationToken
                RequestStorage = deps.RequestStorage
            }
        }

module Notification =
    open EA.Telegram.Domain

    type Dependencies = {
        getRequestChats: Request<Payload> -> Async<Result<Chat list, Error'>>
        setRequestAppointments: ServiceId -> Appointment Set -> Async<Result<Request<Payload> list, Error'>>
        translateMessages: Culture -> Message seq -> Async<Result<Message list, Error'>>
        sendMessages: Message seq -> Async<Result<unit, Error'>>
    }
