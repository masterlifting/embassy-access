[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Command

open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain

module Definitions =
    module Chat =

        type Create = ChatSubscription of ChatId * RequestId
        type CreateOrUpdate = ChatSubscription of ChatId * RequestId
        type Update = Chat of Chat
        type Delete =
            | Chat of ChatId
            | Subscription of ChatId * RequestId

module Chat =
    type Operation =
        | Create of Definitions.Chat.Create
        | CreateOrUpdate of Definitions.Chat.CreateOrUpdate
        | Update of Definitions.Chat.Update
        | Delete of Definitions.Chat.Delete
