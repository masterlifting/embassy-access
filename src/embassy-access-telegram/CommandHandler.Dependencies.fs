[<RequireQualifiedAccess>]
module EA.Telegram.CommandHandler.Dependencies

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain
open EA.Telegram.Dependencies

type GetService =
    { Consumer: Consumer.Dependencies
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            return
                { Consumer = deps
                  getEmbassiesGraph = deps.Persistence.getEmbassyGraph }
        }

type SetService =
    { ConsumerDeps: Consumer.Dependencies
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            return
                { ConsumerDeps = deps
                  getEmbassiesGraph = deps.Persistence.getEmbassyGraph }
        }

type GetEmbassies =
    { ConsumerDeps: Consumer.Dependencies
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            return
                { ConsumerDeps = deps
                  getEmbassiesGraph = deps.Persistence.getEmbassyGraph }
        }

type GetUserEmbassies =
    { ConsumerDeps: Consumer.Dependencies
      getChatEmbassies: ChatId -> Async<Result<EmbassyGraph list, Error'>>
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! chatStorage = deps.Persistence.initChatStorage ()
            let! requestStorage = deps.Persistence.initRequestStorage ()

            let getChat chatId =
                chatStorage
                |> EA.Telegram.DataAccess.Chat.Query.tryFindById chatId
                |> ResultAsync.bind (function
                    | Some chat -> Ok chat
                    | None -> $"{chatId}" |> NotFound |> Error)

            let getEmbassies chat =
                requestStorage
                |> EA.Core.DataAccess.Request.Query.Embassy.findManyByRequestIds chat.Subscriptions

            let getChatEmbassies chatId =
                getChat chatId |> ResultAsync.bindAsync getEmbassies

            return
                { ConsumerDeps = deps
                  getChatEmbassies = getChatEmbassies
                  getEmbassiesGraph = deps.Persistence.getEmbassyGraph }
        }
