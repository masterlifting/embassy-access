module EA.Telegram.Domain

open System.Threading
open Microsoft.Extensions.Configuration
open Infrastructure
open EA.Domain
open Web.Telegram.Domain
open Web.Telegram.Domain.Producer

module Key =

    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    
    [<Literal>]
    let Chats = "chats"

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

type Response =
    | Embassies of (ChatId -> Async<Result<Data, Error'>>)
    | UserEmbassies of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
    | Subscribe of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
    | Countries of (ChatId * int -> Async<Result<Data, Error'>>)
    | Cities of (ChatId * int -> Async<Result<Data, Error'>>)
    | UserCountries of (ChatId * int -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
    | UserCities of (ChatId * int -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
    | SubscriptionRequest of (ChatId * int -> Async<Result<Data, Error'>>)
    | UserSubscriptions of (ChatId * int -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
    | ConfirmAppointment of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)

module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
