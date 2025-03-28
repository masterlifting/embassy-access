[<AutoOpen>]
module EA.Telegram.Domain.Chat

open Infrastructure.Domain
open EA.Core.Domain
open Web.Clients.Domain.Telegram

type Chat = {
    Id: ChatId
    Subscriptions: Set<RequestId>
    Culture: Culture
}
