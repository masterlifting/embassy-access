[<RequireQualifiedAccess>]
module EA.Telegram.Domain.Constants

open Web.Telegram.Domain

let internal ADMIN_CHAT_ID = 379444553L |> ChatId

[<Literal>]
let SERVICE_ROOT_ID = "SRV"

[<Literal>]
let RUSSIAN_NODE_ID = "RUS"
module Endpoint =
    [<Literal>]
    let DELIMITER = "|"
