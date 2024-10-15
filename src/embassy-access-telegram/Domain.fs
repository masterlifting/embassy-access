﻿module EA.Telegram.Domain

open EA.Domain
open Microsoft.Extensions.Configuration
open Web.Telegram.Domain

module Key =
    [<Literal>]
    let internal EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

    [<Literal>]
    let Chats = "chats"

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId> }

module Message =
    open System.Threading

    type PayloadResponse =
        { Config: IConfigurationRoot
          Ct: CancellationToken
          ChatId: ChatId
          Embassy: string
          Country: string
          City: string
          Payload: string }

module External =

    type Chat() =
        member val Id = 0L with get, set
        member val Subscriptions = List.empty<string> with get, set
