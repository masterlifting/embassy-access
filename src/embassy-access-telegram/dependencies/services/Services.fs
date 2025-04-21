[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph () |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)

            return {
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                getServiceNode = getServiceNode
            }
        }
