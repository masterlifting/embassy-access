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
    CT: CancellationToken
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    sendTranslatedMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {

            let getServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph () |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)

            let sendTranslatedMessageRes msg =
                msg |> deps.translateMessageRes chat.Culture |> deps.sendMessageRes

            return {
                CT = deps.CT
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                getServiceNode = getServiceNode
                sendTranslatedMessageRes = sendTranslatedMessageRes
            }
        }
