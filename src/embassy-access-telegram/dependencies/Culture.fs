[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Dependencies
open AIProvider.Services
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      CulturePlaceholder: Culture.Placeholder
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      translate: Culture.Request -> Async<Result<Culture.Response, Error'>> }

    static member create chatId messageId ct =
        fun
            (aiProvider: AIProvider)
            (chatStorage: Chat.ChatStorage)
            (cultureStorage: Culture.Response.Storage)
            (culturePlaceholder: Culture.Placeholder) ->
            let result = ResultBuilder()

            result {

                let getAvailableCultures () =
                    [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

                let setCurrentCulture culture =
                    chatStorage |> Chat.Command.setCulture chatId culture

                let tryGetChat () =
                    chatStorage |> Chat.Query.tryFindById chatId

                let translate request =
                    { Culture.Dependencies.Provider = aiProvider
                      Culture.Dependencies.Storage = cultureStorage }
                    |> Culture.Service.translate request ct

                return
                    { ChatId = chatId
                      MessageId = messageId
                      CancellationToken = ct
                      CulturePlaceholder = culturePlaceholder
                      tryGetChat = tryGetChat
                      getAvailableCultures = getAvailableCultures
                      setCurrentCulture = setCurrentCulture
                      translate = translate }
            }
