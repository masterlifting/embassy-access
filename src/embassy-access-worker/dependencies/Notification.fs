[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Notification.Dependencies

open Infrastructure.Prelude
open EA.Telegram.Dependencies

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! web = Web.Dependencies.create ()
        let! persistence = Persistence.Dependencies.create cfg
        let! aiProvider = AIProvider.Dependencies.create ()

        let! aiProvider = aiProvider.initProvider ()
        let! cultureStorage = persistence.initCultureStorage ()

        let culture: AIProvider.Services.Dependencies.Culture.Dependencies = {
            Provider = aiProvider
            Storage = cultureStorage
        }
        
        let notificationDeps: Notification.Dependencies = {
            translateMessages = telegram.Culture.translateSeq
            setRequestAppointments = persistence.setRequestAppointments
            getRequestChats = persistence.getRequestChats
            sendMessages = web.Telegram.sendMessages
        }

        let tgCulture = EA.Telegram.Dependencies.Culture.Dependencies.create ct culture

        let! tgWeb = EA.Telegram.Dependencies.Web.Dependencies.create ct web.TelegramClient

        let! tgPersistence =
            EA.Telegram.Dependencies.Persistence.Dependencies.create
                ct
                (fun () -> persistence.ChatStorage |> Ok)
                (fun () -> persistence.RequestStorage |> Ok)
                persistence.initServiceGraphStorage
                persistence.initEmbassyGraphStorage

        let result: EA.Telegram.Dependencies.Client.Dependencies = {
            CancellationToken = ct
            Culture = tgCulture
            Web = tgWeb
            Persistence = tgPersistence
        }

        return result
    }
