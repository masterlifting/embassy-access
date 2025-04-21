[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Embassies

open System
open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    Request: Request.Dependencies
    getEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
    getUserEmbassyNode: EmbassyId -> Async<Result<Graph.Node<Embassy> option, Error'>>
} with

    static member create (chat: Chat) (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyGraph () |> ResultAsync.map (Graph.BFS.tryFind embassyId.Value)
                
            let private userSubscriptionsFilter (embassyId: EmbassyId) =
                chat.Subscriptions |> Seq.map _.EmbassyId |> Seq.contains embassyId.Value
            
            let getUserEmbassyNode (embassyId: EmbassyId) =
                deps.getEmbassyGraph () |> ResultAsync.map (Graph.BFS.tryFind embassyId.Value)

            return {
                ChatId = deps.ChatId
                MessageId = deps.MessageId
                Request = deps
                getEmbassyNode = getEmbassyNode
                getUserEmbassyNode = getUserEmbassyNode
            }
        }
