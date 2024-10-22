module EA.Telegram.Domain

open Infrastructure
open EA.Domain
open Web.Telegram.Domain

module Key =
    open System
    open System.Text
    
    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

    [<Literal>]
    let Chats = "chats"

    [<Literal>]
    let SUB = "SUB"

    [<Literal>]
    let INF = "INF"

    [<Literal>]
    let APT = "APT"

    [<Literal>]
    let CNF = "CNF"

    let wrap (values: string seq) = values |> String.concat "|"

    let unwrap (value: string) = value |> fun x -> x.Split '|'

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module Response =
    open System.Threading
    open Web.Telegram.Domain.Producer
    open Microsoft.Extensions.Configuration

    type Text =
        | Embassies of (ChatId -> Async<Result<Data, Error'>>)
        | UserEmbassies of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | Subscribe of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoText

    type Callback =
        | Countries of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | Cities of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | UserCountries of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | UserCities of ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | SubscriptionRequest of ((ChatId * int) -> Async<Result<Data, Error'>>)
        | UserSubscriptions of
            ((ChatId * int) -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | ConfirmAppointment of (ChatId -> IConfigurationRoot -> CancellationToken -> Async<Result<Data, Error'>>)
        | NoCallback


module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
