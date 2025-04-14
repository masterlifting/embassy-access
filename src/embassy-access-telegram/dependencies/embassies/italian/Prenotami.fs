[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Italian.Prenotami

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
    Service: EA.Italian.Services.Domain.Prenotami.Dependencies
} with

    static member create(deps: Italian.Dependencies) = {
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
