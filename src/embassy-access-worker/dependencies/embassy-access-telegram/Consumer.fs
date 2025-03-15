[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Consumer

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = EA.Worker.Dependencies.Web.Dependencies.create ()
        let! persistenceDeps = EA.Worker.Dependencies.Persistence.Dependencies.create cfg
        let! aiProviderDeps = EA.Worker.Dependencies.AIProvider.Dependencies.create ()

        let result: Consumer.Dependencies =
            { CancellationToken = ct
              TelegramClient = webDeps.TelegramClient
              initAIProvider = aiProviderDeps.initProvider
              initCultureStorage = persistenceDeps.initCultureStorage
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok
              initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
              initEmbassyGraphStorage = persistenceDeps.initEmbassyGraphStorage }

        return result
    }
