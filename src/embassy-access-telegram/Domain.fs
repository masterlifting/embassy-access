module EA.Telegram.Domain

open EA.Core.Domain
open Web.Telegram.Domain

module Constants =

    let internal ADMIN_CHAT_ID = 379444553L |> ChatId

    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

    [<Literal>]
    let CHATS_STORAGE_NAME = "chats"

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
