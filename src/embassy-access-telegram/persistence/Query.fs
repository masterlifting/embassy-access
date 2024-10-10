[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Query

open EmbassyAccess

// module Filter =
//     module Chat =
        
module Chat =
    type GetOne = Id of Web.Telegram.Domain.ChatId
    type GetMany = Search of Domain.RequestId
