﻿[<AutoOpen>]
module EA.Telegram.Domain.Chat

open EA.Core.Domain
open Multilang.Domain
open Web.Telegram.Domain

type Chat =
    { Id: ChatId
      Subscriptions: Set<RequestId>
      Culture: Culture }
