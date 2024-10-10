[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Command

open Web.Telegram.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Telegram.Domain

module Options =
    module Chat =

        type Create = ByRequestId of ChatId * RequestId
        type Update = Chat of Chat
        type Delete = ChatId of ChatId

module Chat =
    type Operation =
        | Create of Options.Chat.Create
        | Update of Options.Chat.Update
        | Delete of Options.Chat.Delete
