[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Culture

open Infrastructure.Domain
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Dependencies
open AIProvider.Services

type Dependencies =
    { Placeholder: Culture.Placeholder
      translate: Request -> Async<Result<Response, Error'>> }

    static member create placeholder ct =
        fun (deps: Culture.Dependencies) ->

            let translate request =
                deps |> Culture.Service.translate request ct

            { Placeholder = placeholder
              translate = translate }
