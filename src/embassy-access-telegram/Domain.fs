module EmbassyAccess.Telegram.Domain

open EmbassyAccess.Domain
open Web.Telegram.Domain

[<Literal>]
let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
