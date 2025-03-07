[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Producer

open System.Threading
open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { CancellationToken: CancellationToken
      initTelegramClient: unit -> Result<Web.Telegram.Domain.Client.Client, Error'>
      initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }
