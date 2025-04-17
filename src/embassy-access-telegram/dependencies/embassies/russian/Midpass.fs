[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Midpass

open System.Threading
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    CancellationToken: CancellationToken
    translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
    Service: EA.Russian.Services.Domain.Midpass.Dependencies
} with

    static member create(deps: Russian.Dependencies) = {
        ChatId = deps.Chat.Id
        MessageId = deps.MessageId
        CancellationToken = deps.CancellationToken
        translateMessageRes = deps.Culture.translateRes deps.Chat.Culture
        sendMessageRes = deps.sendMessageRes
        Service = {
            CancellationToken = deps.CancellationToken
            RequestStorage = deps.RequestStorage
        }
    }
