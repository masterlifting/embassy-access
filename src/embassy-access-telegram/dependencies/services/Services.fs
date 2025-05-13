[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Services

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    tryAddSubscription: RequestId -> ServiceId -> EmbassyId -> Async<Result<unit, Error'>>
    deleteSubscription: RequestId -> Async<Result<unit, Error'>>
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    findService: ServiceId -> Async<Result<Service, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Embassy, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {

            let tryFindServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph () |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)

            let tryFindEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyGraph () |> ResultAsync.map (Graph.BFS.tryFind embassyId.Value)

            let findService serviceId =
                tryFindServiceNode serviceId
                |> ResultAsync.bind (function
                    | Some node -> node.Value |> Ok
                    | None -> $"Service '{serviceId.ValueStr}' not found." |> NotFound |> Error)

            let findEmbassy embassyId =
                tryFindEmbassyNode embassyId
                |> ResultAsync.bind (function
                    | Some node -> node.Value |> Ok
                    | None -> $"Embassy '{embassyId.ValueStr}' not found." |> NotFound |> Error)

            let sendTranslatedMessageRes msg =
                msg |> deps.translateMessageRes chat.Culture |> deps.sendMessageRes

            let tryAddSubscription requestId serviceId embassyId =

                let subscriptions =
                    match chat.Subscriptions |> Seq.tryFind (fun s -> s.Id = requestId) with
                    | None -> chat.Subscriptions
                    | Some subscription -> chat.Subscriptions |> Set.remove subscription

                deps.Persistence.initChatStorage ()
                |> ResultAsync.wrap (
                    Storage.Chat.Command.update {
                        chat with
                            Subscriptions =
                                subscriptions
                                |> Set.add {
                                    Id = requestId
                                    ServiceId = serviceId
                                    EmbassyId = embassyId
                                }
                    }
                )
                |> ResultAsync.map ignore

            let deleteSubscription requestId =
                deps.Persistence.initChatStorage ()
                |> ResultAsync.wrap (fun storage ->
                    storage
                    |> Storage.Chat.Command.update {
                        chat with
                            Subscriptions = chat.Subscriptions |> Set.filter (fun s -> s.Id <> requestId)
                    })
                |> ResultAsync.map ignore

            return {
                ct = deps.ct
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                tryAddSubscription = tryAddSubscription
                deleteSubscription = deleteSubscription
                tryFindServiceNode = tryFindServiceNode
                tryFindEmbassyNode = tryFindEmbassyNode
                findService = findService
                findEmbassy = findEmbassy
                sendTranslatedMessageRes = sendTranslatedMessageRes
            }
        }
