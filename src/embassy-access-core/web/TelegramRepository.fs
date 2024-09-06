[<RequireQualifiedAccess>]
module internal EmbassyAccess.Web.TelegramRepository

open Infrastructure
open Web.Telegram

module Request =

    let send ct filter client =
        async { return Error <| NotImplemented $"Telegram send request with filter {filter}" }

    let receive ct response client =
        async { return Error <| NotImplemented $"Telegram receive request with response {response}" }
