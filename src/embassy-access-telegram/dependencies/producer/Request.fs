[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Request

open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { Culture: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      sendMessages: Producer.Message seq -> Async<Result<unit, Error'>> }

    static member create() =
        fun (deps: Producer.Dependencies) ->
            let result = ResultBuilder()

            result {

                let cultureDeps =
                    Culture.Dependencies.create
                        { Placeholder = deps.Culture.Placeholder
                          translate = deps.Culture.translate }

                let! chatStorage = deps.Persistence.initChatStorage ()
                let! requestStorage = deps.Persistence.initRequestStorage ()

                let sendMessages data =
                    deps.initTelegramClient ()
                    |> ResultAsync.wrap (Web.Telegram.Producer.produceSeq data deps.CancellationToken)
                    |> ResultAsync.map ignore

                return
                    { Culture = cultureDeps
                      ChatStorage = chatStorage
                      RequestStorage = requestStorage
                      sendMessages = sendMessages }
            }
