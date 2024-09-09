[<RequireQualifiedAccess>]
module internal EmbassyAccess.Notification.TelegramRepository

open Infrastructure
open EmbassyAccess.Notification
open Web.Telegram.Domain

module Request =
    open Telegram.Bot

    let send ct (notification: Send.Request) (client: Client) =
        async {
            return
                Error
                <| NotImplemented $"Telegram send request with notification {notification}"
        }

    let receive ct listener client =
        async { return Error <| NotImplemented $"Telegram receive request with response {data}" }
