module EA.Telegram.Domain

open EA.Core.Domain
open Web.Telegram.Domain

module Constants =

    let internal ADMIN_CHAT_ID = 379444553L |> ChatId

    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"


type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }