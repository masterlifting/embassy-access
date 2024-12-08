[<RequireQualifiedAccess>]
module EA.Telegram.CommandHandler.Dependencies

open Infrastructure
open EA.Core.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain
open EA.Telegram.Initializer

type GetService =
    { ConsumerDeps: ConsumerDeps
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create(deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {

            let getEmbassiesGraph () =
                deps.Configuration |> EA.Core.Settings.Embassy.getGraph

            return
                { ConsumerDeps = deps
                  getEmbassiesGraph = getEmbassiesGraph }
        }

type SetService =
    { ConsumerDeps: ConsumerDeps
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create (deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {

            let getEmbassiesGraph () =
                deps.Configuration |> EA.Core.Settings.Embassy.getGraph

            return
                { ConsumerDeps = deps
                  getEmbassiesGraph = getEmbassiesGraph }
        }

type GetEmbassies =
    { ConsumerDeps: ConsumerDeps
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create (deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {

            let getEmbassiesGraph () =
                deps.Configuration |> EA.Core.Settings.Embassy.getGraph

            return
                { ConsumerDeps = deps
                  getEmbassiesGraph = getEmbassiesGraph }
        }

type GetUserEmbassies =
    { ConsumerDeps: ConsumerDeps
      getChatEmbassies: ChatId -> Async<Result<EmbassyGraph list, Error'>>
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>> }

    static member create (deps: ConsumerDeps) =
        let result = ResultBuilder()

        result {

            let! chatStorage = deps.Persistence.initChatStorage ()
            let! requestStorage = deps.Persistence.initRequestStorage ()
            let getEmbassiesGraph  = deps.Persistence.getEmbassyGraph
            
            let getChat chatId =
                chatStorage
                |> EA.Telegram.DataAccess.Chat.tryFindById chatId
                |> ResultAsync.bind (function
                    | Some chat -> Ok chat
                    | None -> $"{chatId}" |> NotFound |> Error)

            let getEmbassies chat =
                requestStorage
                |> EA.Core.DataAccess.Request.findEmbassiesByRequestIds chat.Subscriptions

            let getChatEmbassies chatId =
                getChat chatId |> ResultAsync.bindAsync getEmbassies

            return
                { ConsumerDeps = deps
                  getChatEmbassies = getChatEmbassies
                  getEmbassiesGraph = getEmbassiesGraph }
        }
