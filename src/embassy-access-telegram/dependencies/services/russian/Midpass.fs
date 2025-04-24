[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Services.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Dependencies.Services.Russian

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    Service: EA.Russian.Services.Domain.Midpass.Dependencies
} with

    static member create(deps: Russian.Dependencies) =
        "Not implemented" |> NotImplemented |> Error
