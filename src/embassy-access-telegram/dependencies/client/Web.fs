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
        start: (Consumer.Data -> Async<Result<unit, Error'>>) -> Async<Result<unit, Error'>>
        sendMessage: Message -> Async<Result<unit, Error'>>
        sendMessageRes: Async<Result<Message, Error'>> -> ChatId -> Async<Result<unit, Error'>>
        sendMessages: Message seq -> Async<Result<unit, Error'>>
        sendMessagesRes: Async<Result<Message seq, Error'>> -> ChatId -> Async<Result<unit, Error'>>
    } with

        static member create ct =
            fun (client: Telegram.Client) ->

                let start processData =
                    (client, processData) |> Web.Clients.Telegram.Consumer.start ct

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

                {
                    start = start
                    sendMessage = sendMessage
                    sendMessageRes = sendMessageRes
                    sendMessages = sendMessages
                    sendMessagesRes = sendMessagesRes
                }

type Dependencies = {
    Telegram: Telegram.Dependencies
} with

    static member create ct =
        fun (client: Telegram.Client) -> {
            Telegram = Telegram.Dependencies.create ct client
        }
