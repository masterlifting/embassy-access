module EA.Telegram.Features.Dependencies.Embassies.Root

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
    tryFindServiceNode: ServiceId -> Async<Result<Tree.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Tree.Node<Embassy> option, Error'>>
    findService: ServiceId -> Async<Result<Tree.Node<Service>, Error'>>
    findEmbassy: EmbassyId -> Async<Result<Tree.Node<Embassy>, Error'>>
    sendMessage: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {

            let tryFindServiceNode (serviceId: ServiceId) =
                deps.getServiceTree () |> ResultAsync.map (Tree.findNode serviceId.NodeId)

            let tryFindEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyTree () |> ResultAsync.map (Tree.findNode embassyId.NodeId)

            let findService serviceId =
                tryFindServiceNode serviceId
                |> ResultAsync.bind (function
                    | Some node -> node |> Ok
                    | None -> $"Service '{serviceId}' not found." |> NotFound |> Error)

            let findEmbassy embassyId =
                tryFindEmbassyNode embassyId
                |> ResultAsync.bind (function
                    | Some node -> node |> Ok
                    | None -> $"Embassy '{embassyId}' not found." |> NotFound |> Error)

            let culture =
                deps.Culture
                |> EA.Telegram.Features.Dependencies.Culture.Dependencies.create deps.ct

            let sendMessage msg =
                msg |> culture.translateRes chat.Culture |> deps.sendMessageRes

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
                sendMessage = sendMessage
            }
        }
