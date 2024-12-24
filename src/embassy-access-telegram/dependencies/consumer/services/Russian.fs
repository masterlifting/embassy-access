[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Services.Russian

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Consumer

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      ServiceGraph: Async<Result<Graph.Node<ServiceNode>, Error'>>
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

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  initRequestStorage = fun _ -> deps.RequestStorage |> Ok
                  ServiceGraph = serviceGraph
                  getChatRequests = deps.getChatRequests
                  createOrUpdateChat = createOrUpdateChat
                  createOrUpdateRequest = createOrUpdateRequest }
        }
