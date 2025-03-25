[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Midpass

open System.Threading
open Infrastructure.Prelude
open EA.Telegram.Domain

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { Chat = deps.Chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken }
        }
