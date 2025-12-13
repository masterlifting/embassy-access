[<RequireQualifiedAccess>]
module EA.Telegram.Features.Dependencies.Embassies

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Features.Dependencies

type Dependencies = {
    Chat: Chat
    MessageId: int
    Request: Request.Dependencies
    getEmbassyNode: EmbassyId -> Async<Result<Tree.Node<Embassy> option, Error'>>
    getServices: EmbassyId -> Async<Result<Message, Error'>>
    getUserServices: EmbassyId -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyTree () |> ResultAsync.map (Tree.findNode embassyId.NodeId)

            let culture = deps.Culture |> Culture.Dependencies.create deps.ct 

            let sendMessageRes data =
                data |> culture.translateRes chat.Culture |> deps.sendMessageRes

            //TODO: implement these methods
            let getServices (embassyId: EmbassyId) =
                "getServices is not implemented" |> NotImplemented |> Error |> async.Return

            let getUserServices (embassyId: EmbassyId) =
                "getUserServices is not implemented" |> NotImplemented |> Error |> async.Return

            return {
                Chat = chat
                MessageId = deps.MessageId
                Request = deps
                getEmbassyNode = getEmbassyNode
                getServices = getServices
                getUserServices = getUserServices
                sendMessageRes = sendMessageRes
            }
        }
