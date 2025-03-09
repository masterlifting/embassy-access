[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      translate: Culture.Domain.Request -> Async<Result<Culture.Domain.Response, Error'>> }

    static member create chatId messageId ct =
        fun (aiProvider: AIProvider) (chatStorage: Chat.ChatStorage) ->
            let result = ResultBuilder()

            result {

                let getAvailableCultures () =
                    [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

                let setCurrentCulture culture =
                    chatStorage |> Chat.Command.setCulture chatId culture

                let tryGetChat () =
                    chatStorage |> Chat.Query.tryFindById chatId

                let translate request =
                    aiProvider |> Culture.Service.translate request

                return
                    { ChatId = chatId
                      MessageId = messageId
                      CancellationToken = ct
                      tryGetChat = tryGetChat
                      getAvailableCultures = getAvailableCultures
                      setCurrentCulture = setCurrentCulture
                      translate = translate }
            }
