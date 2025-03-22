[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Dependencies
open AIProvider.Services
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open Web.Telegram.Domain

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      translate: Culture.Request -> Async<Result<Culture.Response, Error'>> }

    static member create chatId messageId ct =
        fun (chatStorage: Chat.ChatStorage) (cultureDeps: Culture.Dependencies) ->

            let result = ResultBuilder()

            result {

                let getAvailableCultures () =
                    [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

                let setCurrentCulture culture =
                    chatStorage |> Chat.Command.setCulture chatId culture

                let tryGetChat () =
                    chatStorage |> Chat.Query.tryFindById chatId

                let translate request =
                    cultureDeps |> Culture.Service.translate request ct

                return
                    { ChatId = chatId
                      MessageId = messageId
                      tryGetChat = tryGetChat
                      getAvailableCultures = getAvailableCultures
                      setCurrentCulture = setCurrentCulture
                      translate = translate }
            }
