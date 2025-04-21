[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Russian

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
    getServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    getRequestChats: Request -> Async<Result<Chat list, Error'>>
    getChatRequests: unit -> Async<Result<Request list, Error'>>
    setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>

} with

    static member create (deps: Services.Dependencies) =
        let result = ResultBuilder()

        result {
            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions
                
            let getServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None ->
                        $"Embassy '%s{embassyId.Value}' is not implemented. " + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error)

            return {
                Chat = chat
                MessageId = deps.MessageId
                CancellationToken = deps.ct
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
