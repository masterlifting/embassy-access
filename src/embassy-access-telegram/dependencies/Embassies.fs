[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Embassies

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getEmbassyNode: EmbassyId -> Async<Result<Tree.Node<Embassy> option, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyTree () |> ResultAsync.map (Tree.findNode embassyId.Value)

            let sendMessageRes data =
                data |> deps.translateMessageRes chat.Culture |> deps.sendMessageRes

            return {
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                getEmbassyNode = getEmbassyNode
                sendMessageRes = sendMessageRes
            }
        }
