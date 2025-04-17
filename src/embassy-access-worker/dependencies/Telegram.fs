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

        let culture: AIProvider.Services.Dependencies.Culture.Dependencies = {
            Provider = aiProvider
            Storage = cultureStorage
        }

        let client: EA.Telegram.Dependencies.Client.Dependencies = {
            ct = ct
            Web = EA.Telegram.Dependencies.Web.Dependencies.create ct web.TelegramClient
            Culture = EA.Telegram.Dependencies.Culture.Dependencies.create ct culture
            Persistence = {
                initChatStorage = persistence.initChatStorage
                initServiceStorage = persistence.initServiceStorage
                initEmbassyStorage = persistence.initEmbassyStorage
                RussianStorage = {
                    initKdmidRequestStorage = persistence.RussianStorage.initKdmidRequestStorage
                }
                ItalianStorage = {
                    initPrenotamiRequestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage
                }
            }
        }

        return client
    }
