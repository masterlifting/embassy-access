[<AutoOpen>]
module EA.Telegram.Domain.Chat

open Infrastructure.Domain
open EA.Core.Domain
open Web.Clients.Domain.Telegram

type Subscription = {
    ServiceId: ServiceId
    EmbassyId: EmbassyId
}

type Chat = {
    Id: ChatId
    Subscriptions: Set<Subscription>
    Culture: Culture
}
