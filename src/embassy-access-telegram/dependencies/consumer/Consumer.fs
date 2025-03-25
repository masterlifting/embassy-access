﻿[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer

open System.Threading
open EA.Telegram.Dependencies

type Dependencies =
    { CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      Web: Web.Dependencies
      Persistence: Persistence.Dependencies }
