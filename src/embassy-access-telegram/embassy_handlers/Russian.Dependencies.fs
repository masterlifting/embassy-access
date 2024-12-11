[<RequireQualifiedAccess>]
module EA.Telegram.CommandHandler.Russian.Dependencies

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type GetService =
    { ChatId: ChatId
      MessageId: int
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  getServiceGraph = deps.Persistence.getRussianServiceGraph }
        }

type SetService =
    { ChatId: ChatId
      createOrUpdateChat: Chat -> Async<Result<Chat, Error'>>
      createOrUpdateRequest: Request -> Async<Result<Request, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {
            let! chatStorage = deps.Persistence.initChatStorage ()
            let! requestStorage = deps.Persistence.initRequestStorage ()

            let createOrUpdateChat chat =
                chatStorage
                |> EA.Telegram.DataAccess.Chat.Query.tryFindById chat.Id
                |> ResultAsync.map (function
                    | Some x ->
                        { x with
                            Subscriptions = x.Subscriptions |> Seq.append chat.Subscriptions |> Set.ofSeq }
                    | None -> chat)
                |> ResultAsync.bindAsync (fun chat ->
                    chatStorage |> EA.Telegram.DataAccess.Chat.Command.createOrUpdate chat)

            let createOrUpdateRequest request =
                requestStorage |> EA.Core.DataAccess.Request.Command.createOrUpdate request

            return
                { ChatId = deps.ChatId
                  createOrUpdateChat = createOrUpdateChat
                  createOrUpdateRequest = createOrUpdateRequest
                  getServiceGraph = deps.Persistence.getRussianServiceGraph }
        }
