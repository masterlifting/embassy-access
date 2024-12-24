[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Users

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      EmbassyGraph: Async<Result<Graph.Node<EmbassyNode>, Error'>>
      EmbassiesDeps: Embassies.Dependencies
      getUserEmbassies: ChatId -> Async<Result<Graph.Node<EmbassyNode> seq, Error'>>
      getUserEmbassy: ChatId -> Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>}

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  EmbassyGraph = deps.getEmbassyGraph() }
        }