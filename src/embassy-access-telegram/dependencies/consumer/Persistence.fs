[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Persistence

open Infrastructure.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'> }