[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Request

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    ChatId: Telegram.ChatId
    MessageId: int
    Persistence: Persistence.Dependencies
    initWebBrowser: unit -> Async<Result<Browser.Client, Error'>>
    tryGetChat: unit -> Async<Result<Chat option, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<Embassy>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<Service>, Error'>>
    sendMessage: Telegram.Producer.Message -> Async<Result<unit, Error'>>
    sendMessageRes: Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<unit, Error'>>
    translateMessageRes:
        Culture -> Async<Result<Telegram.Producer.Message, Error'>> -> Async<Result<Telegram.Producer.Message, Error'>>
    getAvailableCultures: unit -> Map<Culture, string>
    setCulture: Culture -> Async<Result<unit, Error'>>
} with

    static member create(payload: Telegram.Consumer.Payload<_>) =
        fun (deps: Client.Dependencies) ->
            let result = ResultBuilder()

            result {
                let tryGetChat () =
                    deps.Persistence.initChatStorage ()
                    |> ResultAsync.wrap (Storage.Chat.Query.tryFindById payload.ChatId)

                let getEmbassyGraph () =
                    deps.Persistence.initEmbassyStorage () |> ResultAsync.wrap EmbassyGraph.get

                let getServiceGraph () =
                    deps.Persistence.initServiceStorage () |> ResultAsync.wrap ServiceGraph.get

                let sendMessageRes data =
                    deps.Web.Telegram.sendMessageRes data payload.ChatId

                let setCulture culture =
                    deps.Persistence.initChatStorage ()
                    |> ResultAsync.wrap (Storage.Chat.Command.setCulture payload.ChatId culture)

                return {
                    ct = deps.ct
                    ChatId = payload.ChatId
                    MessageId = payload.MessageId
                    Persistence = deps.Persistence
                    initWebBrowser = deps.Web.initBrowser
                    tryGetChat = tryGetChat
                    getEmbassyGraph = getEmbassyGraph
                    getServiceGraph = getServiceGraph
                    sendMessage = deps.Web.Telegram.sendMessage
                    sendMessageRes = sendMessageRes
                    translateMessageRes = deps.Culture.translateRes
                    getAvailableCultures = deps.Culture.getAvailable
                    setCulture = setCulture
                }
            }
