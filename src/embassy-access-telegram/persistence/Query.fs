[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Query

open EA.Domain
open EA.Telegram.Domain
open Web.Telegram.Domain

module Filter =
    module Chat =
        module InMemory =
            let hasSubscription (subId: RequestId) (chat: Chat) =
                chat.Subscriptions |> Set.contains subId

module Chat =
    type GetOne = Id of ChatId
    type GetMany = SearchSubscription of RequestId
