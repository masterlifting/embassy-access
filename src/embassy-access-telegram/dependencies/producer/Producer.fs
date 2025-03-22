[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Producer

open System.Threading
open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Dependencies =
    { CancellationToken: CancellationToken
      Culture: AIProvider.Services.Dependencies.Culture.Dependencies
      initTelegramClient: unit -> Result<TelegramClient, Error'>
      initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }
