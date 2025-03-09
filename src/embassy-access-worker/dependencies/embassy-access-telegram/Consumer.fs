[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Consumer

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = EA.Worker.Dependencies.Web.Dependencies.create cfg
        let! persistenceDeps = EA.Worker.Dependencies.Persistence.Dependencies.create cfg
        let! aiDeps = EA.Worker.Dependencies.AIProvider.Dependencies.create cfg

        let result: Consumer.Dependencies =
            { CancellationToken = ct
              TelegramClient = webDeps.TelegramClient
              initAIProvider = aiDeps.initProvider
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok
              initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
              initEmbassyGraphStorage = persistenceDeps.initEmbassyGraphStorage }

        return result
    }
