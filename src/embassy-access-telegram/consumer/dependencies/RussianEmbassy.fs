﻿[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Dependencies.RussianEmbassy

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      ServiceGraph: Async<Result<Graph.Node<ServiceNode>, Error'>>
      getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
      getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>>
      createOrUpdateChat: Chat -> Async<Result<Chat, Error'>>
      createOrUpdateRequest: Request -> Async<Result<Request, Error'>> }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let serviceGraph =
                "RussianServices"
                |> deps.initServiceGraphStorage
                |> ResultAsync.wrap ServiceGraph.get

            let createOrUpdateChat chat =
                deps.ChatStorage |> Chat.Command.createOrUpdate chat

            let createOrUpdateRequest request =
                deps.RequestStorage |> Request.Command.createOrUpdate request

            let getServices serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None -> $"Service with Id {serviceId.Value}" |> NotFound |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  initRequestStorage = fun _ -> deps.RequestStorage |> Ok
                  ServiceGraph = serviceGraph
                  getService = getServices
                  getEmbassy = getEmbassy
                  getChatRequests = deps.getChatRequests
                  createOrUpdateChat = createOrUpdateChat
                  createOrUpdateRequest = createOrUpdateRequest }
        }
