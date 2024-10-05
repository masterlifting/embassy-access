[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Command

open EmbassyAccess

type Chat =
    | Create of Telegram.Domain.Chat
    | Update of Telegram.Domain.Chat
    | Delete of Telegram.Domain.Chat
