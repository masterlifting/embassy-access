[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Producer

open System.Threading
open Infrastructure.Domain
open EA.Telegram.Domain

type Dependencies =
    { CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      Persistence: Persistence.Dependencies
      initTelegramClient: unit -> Result<TelegramClient, Error'> }
