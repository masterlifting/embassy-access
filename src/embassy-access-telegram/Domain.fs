module EA.Telegram.Domain

open Infrastructure
open EA.Domain
open Web.Telegram.Domain

module Key =
    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

    [<Literal>]
    let Chats = "chats"

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module Message =
    open System.Threading
    open Web.Telegram.Domain.Producer
    open Microsoft.Extensions.Configuration

    type Text =
        | SupportedEmbassies of (ChatId -> Data)
        | UserEmbassies of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | SubscriptionResult of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | UserSubscriptions of
            ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoText

    type Callback =
        | SupportedCountries of ((ChatId * int) -> Data)
        | SupportedCities of ((ChatId * int) -> Data)
        | UserCountries of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | UserCities of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoCallback


module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
