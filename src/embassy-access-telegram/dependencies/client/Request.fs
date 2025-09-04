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
    tryGetChat: unit -> Async<Result<Chat option, Error'>>
    getEmbassyTree: unit -> Async<Result<Tree.Node<Embassy>, Error'>>
    getServiceTree: unit -> Async<Result<Tree.Node<Service>, Error'>>
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

                let getEmbassyTree () =
                    deps.Persistence.initEmbassyStorage () |> ResultAsync.wrap EmbassiesTree.get

                let getServiceTree () =
                    deps.Persistence.initServiceStorage () |> ResultAsync.wrap ServicesTree.get

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
                    tryGetChat = tryGetChat
                    getEmbassyTree = getEmbassyTree
                    getServiceTree = getServiceTree
                    sendMessage = deps.Web.Telegram.sendMessage
                    sendMessageRes = sendMessageRes
                    translateMessageRes = deps.Culture.translateRes
                    getAvailableCultures = deps.Culture.getAvailable
                    setCulture = setCulture
                }
            }
