[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Persistence

open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }
