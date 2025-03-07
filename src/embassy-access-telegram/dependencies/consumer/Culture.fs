[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open Multilang.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      ConsumerDeps: Consumer.Dependencies
      TranslationClient: Multilang.Translator.Type
      tryGetChat: unit -> Async<Result<Chat option, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
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

            let! translationClient =
                { Multilang.OpenAI.Domain.Connection.Token = "" }
                |> Multilang.Translator.Connection.OpenAI
                |> Multilang.Translator.init

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  ConsumerDeps = deps
                  TranslationClient = translationClient
                  tryGetChat = tryGetChat
                  getAvailableCultures = getAvailableCultures
                  setCurrentCulture = setCurrentCulture
                  sendResult = deps.sendResult }
        }
