module EA.Telegram.Dependencies.Embassies.Italian.Italian

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies = {
    Chat: Chat
    MessageId: int
    CancellationToken: CancellationToken
    Culture: Culture.Dependencies
    ChatStorage: Chat.ChatStorage
    RequestStorage: Request.RequestStorage
    sendMessages: Producer.Message seq -> Async<Result<unit, Error'>>
    sendMessageRes: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    sendMessagesRes: Async<Result<Producer.Message seq, Error'>> -> Async<Result<unit, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
    getRequestChats: Request -> Async<Result<Chat list, Error'>>
    getChatRequests: unit -> Async<Result<Request list, Error'>>
    setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>
    
} with

    static member create chat (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions
            
            return {
                Chat = chat
                MessageId = deps.MessageId
                CancellationToken = deps.CancellationToken
                Culture = deps.Culture
                ChatStorage = deps.ChatStorage
                RequestStorage = deps.RequestStorage
                sendMessages = deps.sendMessages
                sendMessageRes = deps.sendMessageRes
                sendMessagesRes = deps.sendMessagesRes
                getEmbassyGraph = deps.getEmbassyGraph
                getServiceGraph = deps.getServiceGraph
                getRequestChats = deps.getRequestChats
                setRequestAppointments = deps.setRequestAppointments
                getChatRequests = getChatRequests
            }
        }
