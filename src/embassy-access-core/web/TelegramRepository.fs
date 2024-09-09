[<RequireQualifiedAccess>]
module internal EmbassyAccess.Web.TelegramRepository

open Infrastructure
open Web.Telegram
open EmbassyAccess.Web

module Request =

    let send ct (filter: Filter.Request) client =
        async {
            return
                Error
                <| NotImplemented $"Telegram send request with notification {filter}"
        }

    let receive ct response client =
        async { return Error <| NotImplemented $"Telegram receive request with response {response}" }
