[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      ConsumerDeps: Consumer.Dependencies
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      translate: Culture.Domain.Request -> Async<Result<Culture.Domain.Response, Error'>>
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let getAvailableCultures () =
                [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

            let setCurrentCulture culture =
                deps.ChatStorage |> Chat.Command.setCulture deps.ChatId culture

            let tryGetChat () =
                deps.ChatStorage |> Chat.Query.tryFindById deps.ChatId

            let translate request =
                deps.AIProvider |> Culture.Service.translate request

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  ConsumerDeps = deps
                  tryGetChat = tryGetChat
                  getAvailableCultures = getAvailableCultures
                  setCurrentCulture = setCurrentCulture
                  translate = translate
                  sendResult = deps.sendResult }
        }
