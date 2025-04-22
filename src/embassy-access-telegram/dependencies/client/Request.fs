[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Request

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open AIProvider.Services.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies = {
    ct: CancellationToken
    ChatId: ChatId
    MessageId: int
    tryGetChat: unit -> Async<Result<Chat option, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<Embassy>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<Service>, Error'>>
    sendMessage: Message -> Async<Result<unit, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    setCulture: Culture -> Async<Result<unit, Error'>>
    translate: Culture.Request -> Async<Result<Response, Error'>>
} with

    static member create(payload: Consumer.Payload<_>) =
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

                let translate request =
                    deps.Culture |> AIProvider.Services.Culture.translate request deps.ct

                return {
                    ct = deps.ct
                    ChatId = payload.ChatId
                    MessageId = payload.MessageId
                    tryGetChat = tryGetChat
                    getEmbassyGraph = getEmbassyGraph
                    getServiceGraph = getServiceGraph
                    sendMessage = deps.Web.Telegram.sendMessage
                    sendMessageRes = sendMessageRes
                    setCulture = setCulture
                    translate = translate
                }
            }
