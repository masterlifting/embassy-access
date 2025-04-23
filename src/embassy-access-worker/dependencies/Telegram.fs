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

        let client: EA.Telegram.Dependencies.Client.Dependencies = {
            CT = ct
            Culture =
                EA.Telegram.Dependencies.Culture.Dependencies.create ct {
                    Provider = aiProvider
                    Storage = cultureStorage
                }
            Web = EA.Telegram.Dependencies.Web.Dependencies.create ct web.TelegramClient
            Persistence = {
                initChatStorage = persistence.initChatStorage
                initServiceStorage = persistence.initServiceStorage
                initEmbassyStorage = persistence.initEmbassyStorage
                RussianStorage = {
                    initKdmidRequestStorage = persistence.RussianStorage.initKdmidRequestStorage
                    initMidpassRequestStorage = persistence.RussianStorage.initMidpassRequestStorage
                }
                ItalianStorage = {
                    initPrenotamiRequestStorage = persistence.ItalianStorage.initPrenotamiRequestStorage
                }
            }
        }

        return client
    }
