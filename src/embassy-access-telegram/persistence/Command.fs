[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Command

open EmbassyAccess

type CreateOptions =
    | ByRequestId of int64 * Domain.RequestId

type Chat =
    | Create of CreateOptions
    | Update of Telegram.Domain.Chat
    | Delete of Telegram.Domain.Chat
