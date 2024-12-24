[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Services

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      ServiceGraph: Async<Result<Graph.Node<ServiceNode>, Error'>>
      getServices: unit -> Async<Result<Graph.Node<ServiceNode> seq, Error'>>
      getService: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getEmbassyService: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      RussianDeps: Services.Russian.Dependencies }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let serviceGraph =
                "RussianServices"
                |> deps.initServiceGraphStorage
                |> ResultAsync.wrap ServiceGraph.get

            let! russianServicesDeps = Services.Russian.Dependencies.create deps

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  ServiceGraph = serviceGraph
                  RussianDeps = russianServicesDeps }
        }
