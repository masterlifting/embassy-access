[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Query

open EA

// module Filter =
//     module Chat =

module Chat =
    type GetOne = Id of Web.Telegram.Domain.ChatId
    type GetMany = Search of Domain.RequestId
