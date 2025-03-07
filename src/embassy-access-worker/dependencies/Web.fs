[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Web

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Client

type Dependencies =
    { initTelegramClient: unit -> Result<Client, Error'> }

    static member create() =
        let result = ResultBuilder()

        result {

            let initTelegramClient () =
                EA.Worker.Domain.Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN_KEY
                |> EnvKey
                |> Web.Telegram.Client.init

            return { initTelegramClient = initTelegramClient }
        }
