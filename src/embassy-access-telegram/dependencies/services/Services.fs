[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Domain

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getServiceNode: ServiceId -> Async<Result<Graph.Node<Service> option, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let culture = Culture.Dependencies.create deps

            let getServiceNode (serviceId: ServiceId) =
                deps.getServiceGraph () |> ResultAsync.map (Graph.BFS.tryFind serviceId.Value)

            let sendMessageRes data =
                data |> culture.translateRes chat.Culture |> deps.sendMessageRes

            return {
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                getServiceNode = getServiceNode
                sendMessageRes = sendMessageRes
            }
        }
