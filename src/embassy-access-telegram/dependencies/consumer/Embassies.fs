[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies

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
      ServicesDeps: Services.Dependencies
      getEmbassies: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassy: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassyServices: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode> seq, Error'>>
      getEmbassyService: Graph.NodeId -> Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let! servicesDeps = Services.Dependencies.create deps

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  EmbassyGraph = deps.getEmbassyGraph ()
                  ServicesDeps = servicesDeps }
        }
