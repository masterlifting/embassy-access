[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Web

open Infrastructure.Prelude
open Infrastructure.Domain
open EA.Telegram.Domain

module Telegram =
    open Web.Telegram.Domain
    open Web.Telegram.Domain.Producer

    type Dependencies =
        { Client: TelegramClient
          sendMessage: Message -> Async<Result<unit, Error'>>
          sendMessageRes: Async<Result<Message, Error'>> -> ChatId -> Async<Result<unit, Error'>>
          sendMessages: Message seq -> Async<Result<unit, Error'>>
          sendMessagesRes: Async<Result<Message seq, Error'>> -> ChatId -> Async<Result<unit, Error'>> }

        static member create ct =
            fun (initClient: unit -> Result<TelegramClient, Error'>) ->
                let result = ResultBuilder()

                result {

                    let! client = initClient ()

                    let sendMessage message =
                        client |> Web.Telegram.Producer.produce message ct |> ResultAsync.map ignore

                    let sendMessageRes messageRes chatId =
                        client
                        |> Web.Telegram.Producer.produceResult messageRes chatId ct
                        |> ResultAsync.map ignore

                    let sendMessages messages =
                        client |> Web.Telegram.Producer.produceSeq messages ct |> ResultAsync.map ignore

                    let sendMessagesRes messagesRes chatId =
                        client
                        |> Web.Telegram.Producer.produceResultSeq messagesRes chatId ct
                        |> ResultAsync.map ignore

                    return
                        { Client = client
                          sendMessage = sendMessage
                          sendMessageRes = sendMessageRes
                          sendMessages = sendMessages
                          sendMessagesRes = sendMessagesRes }
                }

type Dependencies =
    { Telegram: Telegram.Dependencies }

    static member create ct =
        fun (initClient: unit -> Result<TelegramClient, Error'>) ->
            let result = ResultBuilder()

            result {

                let! telegramDeps = Telegram.Dependencies.create ct initClient

                return { Telegram = telegramDeps }
            }
