﻿[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Request

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      getRequestChats: Request -> Async<Result<Chat list, Error'>>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      sendMessage: Producer.Message -> Async<Result<unit, Error'>>
      sendMessageRes: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendMessages: Producer.Message seq -> Async<Result<unit, Error'>>
      sendMessagesRes: Async<Result<Producer.Message seq, Error'>> -> Async<Result<unit, Error'>> }

    static member create(payload: Consumer.Payload<_>) =
        fun (deps: Consumer.Dependencies) ->
            let result = ResultBuilder()

            result {

                let tryGetChat () =
                    deps.Persistence.ChatStorage |> Chat.Query.tryFindById payload.ChatId

                let getAvailableCultures () =
                    [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

                let setCurrentCulture culture =
                    deps.Persistence.ChatStorage |> Chat.Command.setCulture payload.ChatId culture

                let sendMessageRes data =
                    deps.Web.Telegram.sendMessageRes data payload.ChatId

                let sendMessagesRes data =
                    deps.Web.Telegram.sendMessagesRes data payload.ChatId

                return
                    { ChatId = payload.ChatId
                      MessageId = payload.MessageId
                      CancellationToken = deps.CancellationToken
                      Culture = deps.Culture
                      ChatStorage = deps.Persistence.ChatStorage
                      RequestStorage = requestStorage
                      getRequestChats = deps.Persistence.getRequestChats
                      getServiceGraph = deps.Persistence.getServiceGraph
                      getEmbassyGraph = deps.Persistence.getEmbassyGraph
                      tryGetChat = tryGetChat
                      getAvailableCultures = getAvailableCultures
                      setCurrentCulture = setCurrentCulture
                      sendMessage = deps.Web.Telegram.sendMessage
                      sendMessageRes = sendMessageRes
                      sendMessages = deps.Web.Telegram.sendMessages
                      sendMessagesRes = sendMessagesRes }
            }
