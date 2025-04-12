[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Kdmid

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
open EA.Russian.Clients.Kdmid
open EA.Russian.Clients.Domain.Kdmid

type Dependencies = {
    Chat: Chat
    MessageId: int
    CancellationToken: CancellationToken
    RequestStorage: Request.RequestStorage
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    sendMessagesRes: Async<Result<Message seq, Error'>> -> Async<Result<unit, Error'>>
    getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
    getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
    getRequest: RequestId -> Async<Result<Request, Error'>>
    createRequest: string * ServiceNode * EmbassyNode * bool * ConfirmationState -> Async<Result<Request, Error'>>
    deleteRequest: RequestId -> Async<Result<unit, Error'>>
    processRequest: Request -> Async<Result<Request, Error'>>
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    translateMessagesRes: Async<Result<Message list, Error'>> -> Async<Result<Message seq, Error'>>
    printPayload: string -> Result<string, Error'>
} with

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {

            let getService serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFind embassyId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Embassy '%s{embassyId.Value}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            let getRequest requestId =
                deps.RequestStorage
                |> Request.Query.tryFindById requestId
                |> ResultAsync.bind (function
                    | None ->
                        $"Request '%s{requestId.ValueStr}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some request -> request |> Ok)

            let createRequest (payload, service: ServiceNode, embassy: EmbassyNode, inBackground, confirmationState) =
                let requestId = RequestId.createNew ()
                let limits = Limit.create (20u<attempts>, TimeSpan.FromDays 1) |> Set.singleton

                deps.ChatStorage
                |> Chat.Command.createChatSubscription deps.Chat.Id requestId
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

            let deleteRequest requestId =
                deps.ChatStorage
                |> Chat.Command.deleteChatSubscription deps.Chat.Id requestId
                |> ResultAsync.bindAsync (fun _ -> deps.RequestStorage |> Request.Command.delete requestId)

            let processRequest request =
                {
                    CancellationToken = deps.CancellationToken
                    RequestStorage = deps.RequestStorage
                }
                |> Client.init
                |> ResultAsync.wrap (Service.tryProcess request)

            let translateMessageRes = deps.Culture.translateRes deps.Chat.Culture

            let translateMessagesRes =
                ResultAsync.map Seq.ofList
                >> deps.Culture.translateSeqRes deps.Chat.Culture
                >> ResultAsync.map Seq.ofList

            return {
                Chat = deps.Chat
                MessageId = deps.MessageId
                CancellationToken = deps.CancellationToken
                RequestStorage = deps.RequestStorage
                sendMessageRes = deps.sendMessageRes
                sendMessagesRes = deps.sendMessagesRes
                getRequest = getRequest
                getService = getService
                getEmbassy = getEmbassy
                createRequest = createRequest
                deleteRequest = deleteRequest
                processRequest = processRequest
                translateMessageRes = translateMessageRes
                translateMessagesRes = translateMessagesRes
                printPayload = Payload.create >> Result.map Payload.print
            }
        }
