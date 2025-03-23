[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Producer

open Infrastructure.Prelude
open EA.Telegram.Dependencies
open EA.Worker.Dependencies

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = Web.Dependencies.create ()
        let! persistenceDeps = Persistence.Dependencies.create cfg
        let! aiProviderDeps = AIProvider.Dependencies.create ()
        let! cultureDeps = Culture.Dependencies.create ct persistenceDeps aiProviderDeps

        let result: EA.Telegram.Dependencies.Producer.Producer.Dependencies =
            { CancellationToken = ct
              Culture =
                { Placeholder = cultureDeps.Placeholder
                  translate = cultureDeps.translate }
              initTelegramClient = fun () -> webDeps.TelegramClient |> Ok
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok }

        return result
    }
