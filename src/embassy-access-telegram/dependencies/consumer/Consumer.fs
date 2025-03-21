[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Consumer

open System.Threading
open Infrastructure.Domain
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { CancellationToken: CancellationToken
      TelegramClient: TelegramClient
      CulturePlaceholder: Culture.Placeholder
      initCultureStorage: unit -> Result<Culture.Response.Storage, Error'>
      initAIProvider: unit -> Result<AIProvider, Error'>
      initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: unit -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      initEmbassyGraphStorage: unit -> Result<EmbassyGraph.EmbassyGraphStorage, Error'> }
