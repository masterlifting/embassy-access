[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Russian

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
} with

    static member create(deps: Services.Dependencies) =
        let result = ResultBuilder()

        result {
            return {
                Chat = deps.Chat
                MessageId = deps.MessageId
                Request = deps.Request
                getServiceNode = deps.getServiceNode
            }
        }
