[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Culture

open Infrastructure.Domain
open AIProvider.Services.Domain
open EA.Telegram.Dependencies

type Dependencies =
    { Placeholder: Culture.Placeholder
      translate: Culture.Request -> Async<Result<Culture.Response, Error'>> }

    static member create(deps: Culture.Dependencies) =
        { Placeholder = deps.Placeholder
          translate = deps.translate }

    member this.toBase() : Culture.Dependencies =
        { Placeholder = this.Placeholder
          translate = this.translate }
