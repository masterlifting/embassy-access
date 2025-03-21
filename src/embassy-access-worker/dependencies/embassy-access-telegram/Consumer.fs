[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Consumer

open Infrastructure.Prelude
open AIProvider.Services.Dependencies
open EA.Telegram.Dependencies.Consumer

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
            |> EA.Telegram.Dependencies.Culture.Dependencies.create ct

        let result: Consumer.Dependencies =
            { CancellationToken = ct
              Culture = cultureDeps
              TelegramClient = webDeps.TelegramClient
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok
              initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
              initEmbassyGraphStorage = persistenceDeps.initEmbassyGraphStorage }

        return result
    }
