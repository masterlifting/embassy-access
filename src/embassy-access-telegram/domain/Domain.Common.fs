[<AutoOpen>]
module EA.Telegram.Domain.Common

open Web.Telegram.Domain

module Constants =

    let internal ADMIN_CHAT_ID = 379444553L |> ChatId

    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
