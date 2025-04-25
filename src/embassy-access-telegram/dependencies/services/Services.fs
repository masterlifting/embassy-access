[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Services

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    tryFindServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    tryFindEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {

            let tryFindServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph () |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)

            let tryFindEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyGraph () |> ResultAsync.map (Graph.BFS.tryFind embassyId.Value)

            let sendTranslatedMessageRes msg =
                msg |> deps.translateMessageRes chat.Culture |> deps.sendMessageRes

            return {
                ct = deps.ct
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                tryFindServiceNode = tryFindServiceNode
                tryFindEmbassyNode = tryFindEmbassyNode
                sendTranslatedMessageRes = sendTranslatedMessageRes
            }
        }
