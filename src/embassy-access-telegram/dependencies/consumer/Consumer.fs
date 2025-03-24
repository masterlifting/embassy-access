[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Consumer

open System.Threading
open EA.Telegram.Domain
open EA.Telegram.Dependencies

type Dependencies =
    { CancellationToken: CancellationToken
      TelegramClient: TelegramClient
      Culture: Culture.Dependencies
      Persistence: Persistence.Dependencies }
