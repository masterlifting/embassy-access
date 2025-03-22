[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Request

open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { Culture: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      sendMessage: Producer.Message -> Async<Result<unit, Error'>>
      sendMessages: Producer.Message seq -> Async<Result<unit, Error'>> }

    static member create ct =
        fun (deps: Producer.Dependencies) ->
            let result = ResultBuilder()

            result {

                let! cultureDeps = Culture.Dependencies.create ct deps.Culture
                let! chatStorage = deps.initChatStorage ()
                let! requestStorage = deps.initRequestStorage ()

                let sendMessage data =
                    deps.initTelegramClient ()
                    |> ResultAsync.wrap (Web.Telegram.Producer.produce data deps.CancellationToken)
                    |> ResultAsync.map ignore

                let sendMessages data =
                    deps.initTelegramClient ()
                    |> ResultAsync.wrap (Web.Telegram.Producer.produceSeq data deps.CancellationToken)
                    |> ResultAsync.map ignore

                return
                    { Culture = cultureDeps
                      ChatStorage = chatStorage
                      RequestStorage = requestStorage
                      sendMessage = sendMessage
                      sendMessages = sendMessages }
            }
