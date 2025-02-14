[<RequireQualifiedAccess>]
module EA.Telegram.Domain.Constants

open Web.Telegram.Domain

[<Literal>]
let internal EN_US_CULTURE = "en-US"

let internal SUPPORTED_CULTURES = [ EN_US_CULTURE; "ru-RU" ] |> Set

let internal ADMIN_CHAT_ID = 379444553L |> ChatId

[<Literal>]
let SERVICE_NODE_ID = "SRV"

[<Literal>]
let RUSSIAN_NODE_ID = "RUS"

module Endpoint =
    [<Literal>]
    let DELIMITER = "|"
