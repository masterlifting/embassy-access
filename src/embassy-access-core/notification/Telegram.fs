[<RequireQualifiedAccess>]
module EmbassyAccess.Notification.Telegram

open Infrastructure
open Web.Telegram.Domain

let internal AdminChatId = 379444553L

let send ct msg =
    EnvKey "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    |> Web.Telegram.Client.create
    |> ResultAsync.wrap (msg |> Web.Telegram.Client.send ct)
