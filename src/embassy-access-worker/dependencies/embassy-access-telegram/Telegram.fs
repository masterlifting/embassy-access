[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Dependencies

open Infrastructure.Prelude
open EA.Worker.Dependencies

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! web = Web.Dependencies.create ()
        let! persistence = Persistence.Dependencies.create cfg
        let! aiProvider = AIProvider.Dependencies.create ()

        let! aiProvider = aiProvider.initProvider ()
        let! cultureStorage = persistence.initCultureStorage ()

        let aiCulture: AIProvider.Services.Dependencies.Culture.Dependencies =
            { Provider = aiProvider
              Storage = cultureStorage }

        let tgCulture = EA.Telegram.Dependencies.Culture.Dependencies.create ct aiCulture

        let! tgWeb = EA.Telegram.Dependencies.Web.Dependencies.create ct web.TelegramClient

        let! tgPersistence =
            EA.Telegram.Dependencies.Persistence.Dependencies.create
                ct
                (fun () -> persistence.ChatStorage |> Ok)
                (fun () -> persistence.RequestStorage |> Ok)
                persistence.initServiceGraphStorage
                persistence.initEmbassyGraphStorage

        let result: EA.Telegram.Dependencies.Client.Dependencies =
            { CancellationToken = ct
              Culture = tgCulture
              Web = tgWeb
              Persistence = tgPersistence }

        return result
    }
