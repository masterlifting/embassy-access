[<RequireQualifiedAccess>]
module EA.Telegram.Producer.Dependencies.Core

open Infrastructure.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }
