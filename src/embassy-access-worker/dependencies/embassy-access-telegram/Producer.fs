[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Producer

open Infrastructure.Prelude
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Producer

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = EA.Worker.Dependencies.Web.Dependencies.create ()
        let! persistenceDeps = EA.Worker.Dependencies.Persistence.Dependencies.create cfg
        let! aiProviderDeps = EA.Worker.Dependencies.AIProvider.Dependencies.create ()
        let! aiProvider = aiProviderDeps.initProvider ()
        let! cultureStorage = persistenceDeps.initCultureStorage ()

        let cultureDeps: AIProvider.Services.Dependencies.Culture.Dependencies =
            { Provider = aiProvider
              Storage = cultureStorage }


        let result: Producer.Dependencies =
            { CancellationToken = ct
              Culture = cultureDeps
              initTelegramClient = fun () -> webDeps.TelegramClient |> Ok
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok }

        return result
    }
