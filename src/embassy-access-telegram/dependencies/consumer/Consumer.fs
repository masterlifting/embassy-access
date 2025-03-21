[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Consumer

open System.Threading
open Infrastructure.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.DataAccess

type Dependencies =
    { CancellationToken: CancellationToken
      TelegramClient: TelegramClient
      Culture: Consumer.Culture.Dependencies
      initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'> }
