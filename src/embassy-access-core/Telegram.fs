[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram

open Infrastructure
open Infrastructure.Logging
open Web.Telegram.Domain

let AdminChatId = ChatId 379444553

module private Sender =

    let send ct msg =
        EnvKey "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (msg |> Web.Telegram.Client.send ct)

module private Receiver =
    open Web.Telegram.Domain.Receive

    let receiveMessage ct message =
        match message with
        | Text message ->
            async {
                $"{message}" |> Log.info
                return Ok()
            }
        | _ -> async { return Error <| NotSupported $"Message type: {message}" }

let send ct message = message |> Sender.send ct

let receive ct data =
    match data with
    | Receive.Data.Message message -> message |> Receiver.receiveMessage ct
    | _ -> async { return Error <| NotSupported $"Listener type: {data}" }
