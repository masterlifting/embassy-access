[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Core

open Infrastructure.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }
