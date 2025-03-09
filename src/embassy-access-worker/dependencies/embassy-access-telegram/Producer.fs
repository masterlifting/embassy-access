[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Telegram.Producer

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Producer

let create cfg ct =
    let result = ResultBuilder()

    result {
        let! webDeps = EA.Worker.Dependencies.Web.Dependencies.create cfg
        let! persistenceDeps = EA.Worker.Dependencies.Persistence.Dependencies.create cfg

        let result: Producer.Dependencies =
            { CancellationToken = ct
              initTelegramClient = fun () -> webDeps.TelegramClient |> Ok
              initChatStorage = fun () -> persistenceDeps.ChatStorage |> Ok
              initRequestStorage = fun () -> persistenceDeps.RequestStorage |> Ok }

        return result
    }
