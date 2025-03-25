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

    static member create ct placeholder =
        fun (deps: Culture.Dependencies) ->
            { Placeholder = placeholder
              translate = fun request -> deps |> AIProvider.Services.Culture.Service.translate request ct }
