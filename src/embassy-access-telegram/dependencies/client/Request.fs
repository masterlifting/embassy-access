[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Request

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    ChatId: ChatId
    MessageId: int
    ChatStorage: Chat.Storage
    Client: Client.Dependencies
    tryGetChat: unit -> Async<Result<Chat option, Error'>>
    RequestStorage: Request.Storage
    getRequestChats: Request -> Async<Result<Chat list, Error'>>
    setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<Embassy>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<Service>, Error'>>
    setCurrentCulture: Culture -> Async<Result<unit, Error'>>
    sendMessage: Message -> Async<Result<unit, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    sendMessages: Message seq -> Async<Result<unit, Error'>>
    sendMessagesRes: Async<Result<Message seq, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create(payload: Consumer.Payload<_>) =
        fun (deps: Client.Dependencies) ->
            let result = ResultBuilder()

            result {

                let! chatStorage = deps.Persistence.initChatStorage ()

                let tryGetChat () =
                    chatStorage |> Storage.Chat.Query.tryFindById payload.ChatId

                let sendMessageRes data =
                    deps.Web.Telegram.sendMessageRes data payload.ChatId

                let sendMessagesRes data =
                    deps.Web.Telegram.sendMessagesRes data payload.ChatId
                    
                return {
                    ct = deps.ct
                    ChatId = payload.ChatId
                    MessageId = payload.MessageId
                    Culture = deps.Culture
                    ChatStorage = chatStorage
                    RequestStorage = deps.Persistence.RequestStorage
                    getRequestChats = deps.Persistence.getRequestChats
                    setRequestAppointments = deps.Persistence.setRequestAppointments
                    getServiceGraph = deps.Persistence.getServiceGraph
                    getEmbassyGraph = deps.Persistence.getEmbassyGraph
                    tryGetChat = tryGetChat
                    setCurrentCulture = setCurrentCulture
                    sendMessage = deps.Web.Telegram.sendMessage
                    sendMessageRes = sendMessageRes
                    sendMessages = deps.Web.Telegram.sendMessages
                    sendMessagesRes = sendMessagesRes
                }
            }
