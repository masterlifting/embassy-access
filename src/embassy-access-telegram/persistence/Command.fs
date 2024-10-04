[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Telegram.Command

open EmbassyAccess

type Request =
    | Create of Domain.Request
    | Update of Domain.Request
    | Delete of Domain.Request
