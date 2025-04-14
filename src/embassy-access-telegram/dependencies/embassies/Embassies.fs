[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Embassies

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Dependencies.Embassies.Italian

type Dependencies = {
    Chat: Chat
    MessageId: int
    CancellationToken: CancellationToken
    Culture: Culture.Dependencies
    Russian: Russian.Dependencies
    Italian: Italian.Dependencies
    getServiceNode: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
    getEmbassyNode: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
    getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
    getEmbassyServiceGraph: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    sendMessagesRes: Async<Result<Message seq, Error'>> -> Async<Result<unit, Error'>>
    getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
    getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
    getChatRequests: unit -> Async<Result<Request list, Error'>>
    getRequest: RequestId -> Async<Result<Request, Error'>>
    createRequest: string * ServiceNode * EmbassyNode * bool * ConfirmationState -> Async<Result<Request, Error'>>
    deleteRequest: RequestId -> Async<Result<unit, Error'>>
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    translateMessagesRes: Async<Result<Message list, Error'>> -> Async<Result<Message seq, Error'>>
} with

    static member create chat (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let! russianDeps = Russian.Dependencies.create chat deps
            let! italianDeps = Italian.Dependencies.create chat deps

            let getEmbassyNode embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind embassyId)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None ->
                        $"Embassy '%s{embassyId.Value}' is not implemented. " + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error)

            let getEmbassyServiceGraph embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"Services of Embassy '%s{embassyId.Value}' is not implemented. "
                        + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                        |> async.Return
                    | Some node ->
                        // try to get the countryId from the embassyId. It should be the second part of the embassyId
                        match node.Id.TryGetPart 1 with
                        | None ->
                            $"Services of Embassy '%s{embassyId.Value}' for the chat '%s{deps.ChatId.ValueStr}' is not implemented. "
                            + NOT_IMPLEMENTED
                            |> NotImplemented
                            |> Error
                            |> async.Return
                        | Some countryId ->
                            let serviceId =
                                [ Services.ROOT_ID |> Graph.NodeIdValue; countryId ] |> Graph.NodeId.combine

                            deps.getServiceGraph ()
                            |> ResultAsync.map (Graph.BFS.tryFind serviceId)
                            |> ResultAsync.bind (function
                                | None ->
                                    $"Services of Embassy '%s{embassyId.Value}' for the chat '%s{deps.ChatId.ValueStr}' is not implemented. "
                                    + NOT_IMPLEMENTED
                                    |> NotImplemented
                                    |> Error
                                | Some serviceNode -> serviceNode |> Ok))

            let getServiceNode serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for the chat '%s{deps.ChatId.ValueStr}' is not implemented. "
                        + NOT_IMPLEMENTED
                        |> NotImplemented
                        |> Error
                    | Some serviceNode -> serviceNode |> Ok)

            let getService serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for the chat '%s{chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind embassyId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Embassy '%s{embassyId.Value}' for the chat '%s{chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            let getRequest requestId =
                deps.RequestStorage
                |> Request.Query.tryFindById requestId
                |> ResultAsync.bind (function
                    | None ->
                        $"Request '%s{requestId.ValueStr}' for the chat '%s{chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some request -> request |> Ok)

            let createRequest (payload, service: ServiceNode, embassy: EmbassyNode, inBackground, confirmationState) =
                let requestId = RequestId.createNew ()
                let limits = Limit.create (20u<attempts>, TimeSpan.FromDays 1) |> Set.singleton

                deps.ChatStorage
                |> Chat.Command.createChatSubscription chat.Id requestId
                |> ResultAsync.bindAsync (fun _ ->
                    deps.RequestStorage
                    |> Request.Command.create {
                        Id = requestId
                        Service = {
                            Id = service.Id
                            Name = service.Name
                            Payload = payload
                            Description = service.Description
                            Embassy = embassy
                        }
                        ProcessState = Ready
                        IsBackground = inBackground
                        Limits = limits
                        ConfirmationState = confirmationState
                        Appointments = Set.empty<Appointment>
                        Modified = DateTime.UtcNow
                    })
            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions

            let deleteRequest requestId =
                deps.ChatStorage
                |> Chat.Command.deleteChatSubscription chat.Id requestId
                |> ResultAsync.bindAsync (fun _ -> deps.RequestStorage |> Request.Command.delete requestId)

            let translateMessageRes = deps.Culture.translateRes chat.Culture

            let translateMessagesRes =
                ResultAsync.map Seq.ofList
                >> deps.Culture.translateSeqRes chat.Culture
                >> ResultAsync.map Seq.ofList

            return {
                Chat = chat
                MessageId = deps.MessageId
                CancellationToken = deps.CancellationToken
                Culture = deps.Culture
                Russian = russianDeps
                Italian = italianDeps
                getEmbassiesGraph = deps.getEmbassyGraph
                getEmbassyNode = getEmbassyNode
                getServiceNode = getServiceNode
                getEmbassyServiceGraph = getEmbassyServiceGraph
                translateMessageRes = translateMessageRes
                translateMessagesRes = translateMessagesRes
                sendMessageRes = deps.sendMessageRes
                sendMessagesRes = deps.sendMessagesRes
                getService = getService
                getEmbassy = getEmbassy
                getChatRequests = getChatRequests
                getRequest = getRequest
                createRequest = createRequest
                deleteRequest = deleteRequest
            }
        }
