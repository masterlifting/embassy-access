[<AutoOpen>]
module EA.Telegram.Domain.Chat

open Infrastructure.Domain
open Web.Clients.Domain.Telegram

type Chat = {
    Id: ChatId
    Subscriptions: Set<Subscription>
    Culture: Culture
}
