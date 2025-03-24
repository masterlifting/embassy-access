[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Consumer

open Infrastructure.Prelude
open EA.Worker.Dependencies

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = Web.Dependencies.create ()
        let! persistenceDeps = Persistence.Dependencies.create cfg
        let! aiProviderDeps = AIProvider.Dependencies.create ()
        let! cultureDeps = Culture.Dependencies.create ct persistenceDeps aiProviderDeps

        let result: EA.Telegram.Dependencies.Consumer.Consumer.Dependencies =
            { CancellationToken = ct
              TelegramClient = webDeps.TelegramClient
              Culture =
                { Placeholder = cultureDeps.Placeholder
                  translate = cultureDeps.translate }
              Persistence =
                { initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
                  initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok
                  initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
                  initEmbassyGraphStorage = persistenceDeps.initEmbassyGraphStorage } }

        return result
    }
