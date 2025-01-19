[<RequireQualifiedAccess>]
module EA.Telegram.Domain.Constants

open Web.Telegram.Domain

let internal ADMIN_CHAT_ID = 379444553L |> ChatId

module Endpoint =
    [<Literal>]
    let DELIMITER = "|"
