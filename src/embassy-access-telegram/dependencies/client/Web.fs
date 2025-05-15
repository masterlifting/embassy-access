[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Web

open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Clients.Domain

module Telegram =
    open Web.Clients.Telegram
    open Web.Clients.Domain.Telegram
    open Web.Clients.Domain.Telegram.Producer

    type Dependencies = {
        initConsumer: (Consumer.Data -> Async<Result<unit, Error'>>) -> Telegram.Consumer.Handler
        sendMessage: Message -> Async<Result<unit, Error'>>
        sendMessageRes: Async<Result<Message, Error'>> -> ChatId -> Async<Result<unit, Error'>>
        sendMessages: Message seq -> Async<Result<unit, Error'>>
        sendMessagesRes: Async<Result<Message seq, Error'>> -> ChatId -> Async<Result<unit, Error'>>
    } with

        static member create ct =
            fun (client: Telegram.Client) ->
                let sendMessage message =
                    client |> Producer.produce message ct |> ResultAsync.map ignore

                let sendMessageRes messageRes chatId =
                    client |> Producer.produceResult messageRes chatId ct |> ResultAsync.map ignore

                let sendMessages messages =
                    client |> Producer.produceSeq messages ct |> ResultAsync.map ignore

                let sendMessagesRes messagesRes chatId =
                    client
                    |> Producer.produceResultSeq messagesRes chatId ct
                    |> ResultAsync.map ignore

                let initTelegramConsumer handler =
                    Telegram.Consumer.Handler(client, handler)

                {
                    initConsumer = initTelegramConsumer
                    sendMessage = sendMessage
                    sendMessageRes = sendMessageRes
                    sendMessages = sendMessages
                    sendMessagesRes = sendMessagesRes
                }

type Dependencies = {
    Telegram: Telegram.Dependencies
    initBrowser: unit -> Async<Result<Browser.Client, Error'>>
} with

    static member create ct =
        fun (client: Telegram.Client) initBrowser -> {
            Telegram = Telegram.Dependencies.create ct client
            initBrowser = initBrowser
        }
