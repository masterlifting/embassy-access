[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Producer

open Infrastructure.Prelude
open AIProvider.Services.Dependencies
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

        let! cultureDeps =
            { Culture.Provider = aiProvider
              Culture.Storage = cultureStorage
              Culture.Placeholder = aiProviderDeps.CulturePlaceholder }
            |> Culture.Dependencies.create ct

        let result: Producer.Dependencies =
            { CancellationToken = ct
              Culture = cultureDeps
              initTelegramClient = fun () -> webDeps.TelegramClient |> Ok
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok }

        return result
    }
