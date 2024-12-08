[<RequireQualifiedAccess>]
module EA.Telegram.CommandHandler.Russian.Dependencies

open Infrastructure
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Initializer

type GetService =
    { ChatId: ChatId
      MessageId: int
      getServiceInfoGraph: unit -> Async<Result<Graph.Node<EA.Embassies.Russian.Domain.ServiceInfoGraph>, Error'>> }

    static member create (deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {

            let getServiceInfoGraph () =
                deps.Configuration |> EA.Embassies.Russian.Settings.ServiceInfo.getGraph

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  getServiceInfoGraph = getServiceInfoGraph }
        }

type SetService =
    { ChatId: ChatId
      createOrUpdateChat: Chat -> Async<Result<Chat, Error'>>
      createOrUpdateRequest: Request -> Async<Result<Request, Error'>>
      getServiceInfoGraph: unit -> Async<Result<Graph.Node<EA.Embassies.Russian.Domain.ServiceInfoGraph>, Error'>> }

    static member create (deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {
            let! chatStorage = deps.Persistence.initChatStorage ()
            let! requestStorage = deps.Persistence.initRequestStorage ()
            let getServiceInfoGraph = deps.Persistence.getRussianServiceGraph

            let createOrUpdateChat chat =
                chatStorage
                |> EA.Telegram.DataAccess.Chat.tryFindById chat.Id
                |> ResultAsync.map (function
                    | Some x ->
                        { x with
                            Subscriptions = x.Subscriptions |> Seq.append chat.Subscriptions |> Set.ofSeq }
                    | None -> chat)
                |> ResultAsync.bindAsync (fun chat -> chatStorage |> EA.Telegram.DataAccess.Chat.createOrUpdate chat)

            let createOrUpdateRequest request =
                requestStorage |> EA.Core.DataAccess.Request.createOrUpdate request

            return
                { ChatId = deps.ChatId
                  createOrUpdateChat = createOrUpdateChat
                  createOrUpdateRequest = createOrUpdateRequest
                  getServiceInfoGraph = getServiceInfoGraph }
        }
