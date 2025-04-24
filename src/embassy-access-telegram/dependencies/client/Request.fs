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
    CT: CancellationToken
    ChatId: ChatId
    MessageId: int
    tryGetChat: unit -> Async<Result<Chat option, Error'>>
    getEmbassyGraph: unit -> Async<Result<Graph.Node<Embassy>, Error'>>
    getServiceGraph: unit -> Async<Result<Graph.Node<Service>, Error'>>
    sendMessage: Message -> Async<Result<unit, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    translateMessageRes: Culture -> Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    getAvailableCultures: unit -> Map<Culture, string>
    setCulture: Culture -> Async<Result<unit, Error'>>
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

                let spreadMessages data =
                    data
                    |> ResultAsync.map (Seq.groupBy fst)
                    |> ResultAsync.map (Seq.map (fun (culture, group) -> culture, group |> Seq.map snd |> List.ofSeq))
                    |> ResultAsync.map (Seq.map (fun (culture, group) -> deps.Culture.translateSeq culture group))
                    |> ResultAsync.bindAsync (Async.Parallel >> Async.map Result.choose)
                    |> ResultAsync.map (Seq.collect id)
                    |> ResultAsync.bindAsync deps.Web.Telegram.sendMessages

                return {
                    CT = deps.CT
                    ChatId = payload.ChatId
                    MessageId = payload.MessageId
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
