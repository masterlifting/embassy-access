[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      Placeholder: Culture.Placeholder
      translate: Culture.Request -> Async<Result<Culture.Response, Error'>>
      getAvailableCultures: unit -> Async<Result<Map<Culture, string>, Error'>>
      setCurrentCulture: Culture -> Async<Result<unit, Error'>>
      tryGetChat: unit -> Async<Result<Chat option, Error'>> }

    static member create chatId messageId =
        fun (chatStorage: Chat.ChatStorage) (cultureDeps: Culture.Dependencies) ->

            let result = ResultBuilder()

            result {

                let getAvailableCultures () =
                    [ English, "English"; Russian, "Русский" ] |> Map |> Ok |> async.Return

                let setCurrentCulture culture =
                    chatStorage |> Chat.Command.setCulture chatId culture

                let tryGetChat () =
                    chatStorage |> Chat.Query.tryFindById chatId

                return
                    { ChatId = chatId
                      MessageId = messageId
                      Placeholder = cultureDeps.Placeholder
                      translate = cultureDeps.translate
                      tryGetChat = tryGetChat
                      getAvailableCultures = getAvailableCultures
                      setCurrentCulture = setCurrentCulture }
            }

    member this.toProducer() : Producer.Culture.Dependencies =
        { Placeholder = this.Placeholder
          translate = this.translate }
