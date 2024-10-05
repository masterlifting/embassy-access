module EmbassyAccess.Telegram.Domain

open EmbassyAccess.Domain

[<Literal>]
let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

type Chat =
    { Id: int64
      Subscriptions: Set<RequestId> }

module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = Seq.empty with get, set
